using System;
using UnityEngine;
using System.Net;

namespace X
{
    public class DnsEx
    {
        public static IPAddress GetIPAddress(string host)
        {
            var hostEntry = Dns.GetHostEntry(host);
            foreach (var ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip;
            }

            return null;
        }

        public static IPAddress GetIPAddressByHttpDns(string host)
        {
            var webclient = new System.Net.WebClient();
            var ip = webclient.DownloadString("http://119.29.29.29/d?dn=" + host);

            try
            {
                return IPAddress.Parse(ip);
            }
            catch (Exception e)
            {
                Debug.LogWarning(string.Format("GetIPAddressByHttpDns failed.host:[{0}] ip:[{1}]", host, ip));
                throw e;
            }
        }

        public class IPAddressAsyn
        {
            IPAddress IPAddress;
            Exception Exception;
            WebRequest WebRequest;
            WebResponse WebResponse;
            System.IO.Stream Stream;
            byte[] Buffer = new byte[128];
            int BufferOffset = 0;

            public IPAddressAsyn(string url, int millisecondsTimeout)
            {
                WebRequest = WebRequest.Create(url);
                WebRequest.Timeout = millisecondsTimeout;
                WebRequest.BeginGetResponse(GetResponseCallback, null);
            }

            void GetResponseCallback(IAsyncResult asyncResult)
            {
                try
                {
                    WebResponse = WebRequest.EndGetResponse(asyncResult);
                    Stream = WebResponse.GetResponseStream();

                    BufferOffset = 0;
                    Stream.BeginRead(Buffer, BufferOffset, Buffer.Length, ReadCallback, null);
                }
                catch (System.Exception e)
                {
                    Exception = e;
                }
            }

            void ReadCallback(IAsyncResult asyncResult)
            {
                try
                {
                    var size = Stream.EndRead(asyncResult);
                    if (size <= 0)
                    {
                        var ip = System.Text.Encoding.ASCII.GetString(Buffer);
                        IPAddress = IPAddress.Parse(ip);
                        return;
                    }

                    BufferOffset += size;
                    Stream.BeginRead(Buffer, BufferOffset, Buffer.Length - BufferOffset, ReadCallback, null);
                }
                catch (System.Exception e)
                {
                    Exception = e;
                }
            }

            public IPAddress GetIPAddress()
            {
                if (Exception != null)
                    throw Exception;
                return IPAddress;
            }
        }

        public static IPAddressAsyn GetIPAddressAsynByHttpDns(string host, int millisecondsTimeout)
        {
            var ret = new IPAddressAsyn("http://119.29.29.29/d?dn=" + host, millisecondsTimeout);
            return ret;
        }
    }
}