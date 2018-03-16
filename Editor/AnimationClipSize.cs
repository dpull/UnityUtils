using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;

namespace X
{
    public class AnimationClipToolMenu
    {
        private static bool IsSupport(AnimationClipStatsPublic stats)
        {
            if (stats.size == 0)
                return false;

            if (stats.denseCurves != 0 || stats.constantCurves != 0)
                return false;

            if (stats.totalCurves != (stats.positionCurves * 3 + stats.scaleCurves * 3 + stats.quaternionCurves * 4))
                return false;
            return true;
        }

        [MenuItem("Tools/Test select anim size")]
        public static void TestAnimSize()
        {
            var animationClip = Selection.activeObject as AnimationClip;
            if (animationClip == null)
            {
                Debug.Log("AnimationClip not selected");
                return;
            }

            var stats = AnimationClipStatsPublic.Get(animationClip);
            if (!IsSupport(stats))
            {
                Debug.Log("Unsupported AnimationClip");
                return;
            }

            var builder = new AnimationClipBuilder(animationClip);
            var mystats = builder.GetStats();

            Debug.LogFormat("{0}\t{1}\t{2}\t{3}", animationClip.name, stats.size, mystats.size,
                stats.size - mystats.size);
        }

        [MenuItem("Tools/Test all anim size")]
        public static void TestAllAnimSize()
        {
            var support = 0;
            var correct = 0;

            var animationClipGuids = AssetDatabase.FindAssets("t:AnimationClip");
            var animationClipGuidCount = animationClipGuids.Length;

            for (var i = 0; i < animationClipGuidCount; ++i)
            {
                var animationClipGuid = animationClipGuids[i];
                var animationClipPath = AssetDatabase.GUIDToAssetPath(animationClipGuid);
                var animationClip =
                    AssetDatabase.LoadAssetAtPath(animationClipPath, typeof(AnimationClip)) as AnimationClip;
                var stats = AnimationClipStatsPublic.Get(animationClip);
                if (!IsSupport(stats))
                    continue;

                try
                {
                    EditorUtility.DisplayProgressBar("Process",
                        string.Format("[{0}/{1}] {2}\n", i, animationClipGuidCount, animationClip.name),
                        ((float) i) / animationClipGuidCount);

                    var builder = new AnimationClipBuilder(animationClip);
                    var mystats = builder.GetStats();

                    support++;
                    if (stats.size == mystats.size)
                    {
                        correct++;
                        continue;
                    }

                    Debug.Log(
                        string.Format("{0}\t{1}\t{2}\t{3}", animationClip.name, stats.size, mystats.size,
                            stats.size - mystats.size), animationClip);
                }
                catch (Exception e)
                {
                    Debug.Log(string.Format("{0}, Exception:{1}", animationClip.name, e.Message), animationClip);
                }
            }

            Debug.LogFormat("Total: {0} Support: {1} Correct:{2}", animationClipGuidCount, support, correct);
            EditorUtility.ClearProgressBar();
        }
    }

    public struct AnimationClipStatsPublic
    {
        public int size;
        public int positionCurves;
        public int quaternionCurves;
        public int eulerCurves;
        public int scaleCurves;
        public int muscleCurves;
        public int genericCurves;
        public int pptrCurves;
        public int totalCurves;
        public int constantCurves;
        public int denseCurves;
        public int streamCurves;

        public static AnimationClipStatsPublic Get(AnimationClip animationClip)
        {
            if (MethodGetAnimationClipStats == null)
                MethodGetAnimationClipStats = typeof(AnimationUtility).GetMethod("GetAnimationClipStats",
                    BindingFlags.Static | BindingFlags.NonPublic);

            var stats = MethodGetAnimationClipStats.Invoke(null, new object[] {animationClip});
            var size = Marshal.SizeOf(stats);
            var type = typeof(AnimationClipStatsPublic);
            if (size != Marshal.SizeOf(type))
                throw new TargetException();

            var buffer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(stats, buffer, false);

            var reult = (Marshal.PtrToStructure(buffer, type));
            Marshal.FreeHGlobal(buffer);
            return (AnimationClipStatsPublic) reult;
        }

        static MethodInfo MethodGetAnimationClipStats = null;
    }

    class AnimationClipBuilder
    {
        private const bool ENABLE_DENSE = false;

        enum ClipOptType
        {
            InvalidCurve = -1,
            StreamedClip = 0,
            DenseClip,
            ConstantClip,
            ClipOptCount
        };

        class Curve
        {
            public Curve()
            {
                keys = new List<KeyframeContainer<Vector4>>();
            }

            public string path { get; set; }
            public int preInfinity { get; set; }
            public int postInfinity { get; set; }
            public List<KeyframeContainer<Vector4>> keys { get; private set; }
            public ClipOptType clipType { get; set; }
        }

        class KeyframeContainer<T>
        {
            public float time;
            public T value;
            public T inSlope;
            public T outSlope;
        }

        struct GenericBinding // 4 + 4 + 4 + 8 + 2 + 1 + 1 = 24
        {
            UInt32 path;
            UInt32 attribute;
            UInt32 script;
            UInt64 scriptFile;
            UInt16 classID;
            Byte customType;
            Byte isPPtrCurve;
        }

        class BuildCurveKey
        {
            public float time;
            public int curveIndex;
            public float[] coeff = new float[4];
        }

        struct CurveDetail
        {
            public int StreamedClipCurveIndex;
            public int DenseClipCurveIndex;
            public int ConstantCurveIndex;
        }

        class Curves
        {
            public List<Curve> PositionCurves = new List<Curve>();
            public List<Curve> ScaleCurves = new List<Curve>();
            public List<Curve> RotationCurves = new List<Curve>();
            public List<Curve> FloatCurves;
            public List<Curve> PPtrCurves;
        }

        private AnimationClip AnimationClip;
        private List<Curve> PositionCurves = new List<Curve>();
        private List<Curve> ScaleCurves = new List<Curve>();
        private List<Curve> RotationCurves = new List<Curve>();
        private List<Curve> FloatCurves;
        private List<Curve> PPtrCurves;

        public AnimationClipBuilder(AnimationClip animationClip)
        {
            this.AnimationClip = animationClip;
            BuildCurves();
        }

        public AnimationClipStatsPublic GetStats()
        {
            AnimationClipStatsPublic stats = new AnimationClipStatsPublic();
            stats.positionCurves = PositionCurves.Count;
            stats.scaleCurves = ScaleCurves.Count;
            stats.quaternionCurves = RotationCurves.Count;
            stats.totalCurves = stats.positionCurves * 3 + stats.scaleCurves * 3 + stats.quaternionCurves * 4;

            var groupedCurves = GetGroupedCurves();

            var streamedKeys = new List<BuildCurveKey>();
            CurveDetail curveDetail = new CurveDetail();

            CalcStats(PositionCurves, 3, ref stats, ref curveDetail, streamedKeys);
            CalcStats(ScaleCurves, 3, ref stats, ref curveDetail, streamedKeys);
            CalcStats(RotationCurves, 4, ref stats, ref curveDetail, streamedKeys);

            var bindingSize =
                stats.positionCurves * 24 + stats.scaleCurves * 24 + stats.quaternionCurves * 24; // GenericBinding(24)
            var streamedSize = CalcStreamedClipSize(streamedKeys);
            var constantSize = sizeof(UInt32) + curveDetail.ConstantCurveIndex * sizeof(float);
            var densetSize = 5 * sizeof(UInt32) + curveDetail.DenseClipCurveIndex * sizeof(float);

            var guessBase = 2168;

            var streamedClipCurves = groupedCurves[(int) ClipOptType.StreamedClip];
            var guessCurvesSize = streamedClipCurves.PositionCurves.Count * 60 +
                                  streamedClipCurves.ScaleCurves.Count * 60 +
                                  streamedClipCurves.RotationCurves.Count * 88;

            stats.size = bindingSize + streamedSize + constantSize + densetSize + guessBase + guessCurvesSize;
            // var count = GetPathCount();
            // var guessPath = 8 * count * ( (curveDetail.ConstantCurveIndex > 0 ? 1 : 0) + (curveDetail.StreamedClipCurveIndex > 0 ? 1 : 0) - 1);
            // stats.size += guessPath;
            return stats;
        }

        private static int AlignSize(int size)
        {
            var tmp1 = (float) size;
            tmp1 = tmp1 / 8f + 0.5f;
            var tmp2 = (int) tmp1 * 8;
            return tmp2;
        }

        private Curves[] GetGroupedCurves()
        {
            var grouped = new Curves[(int) ClipOptType.ClipOptCount];
            for (int i = 0; i < (int) ClipOptType.ClipOptCount; i++)
                grouped[i] = new Curves();

            foreach (var curve in PositionCurves)
            {
                grouped[(int) curve.clipType].PositionCurves.Add(curve);
            }

            foreach (var curve in ScaleCurves)
            {
                grouped[(int) curve.clipType].ScaleCurves.Add(curve);
            }

            foreach (var curve in RotationCurves)
            {
                grouped[(int) curve.clipType].RotationCurves.Add(curve);
            }

            return grouped;
        }

        private int GetPathCount()
        {
            var names = new Dictionary<string, int>();
            foreach (var curve in PositionCurves)
            {
                if (!names.ContainsKey(curve.path))
                    names.Add(curve.path, 0);
            }

            foreach (var curve in ScaleCurves)
            {
                if (!names.ContainsKey(curve.path))
                    names.Add(curve.path, 0);
            }

            foreach (var curve in RotationCurves)
            {
                if (!names.ContainsKey(curve.path))
                    names.Add(curve.path, 0);
            }

            return names.Count;
        }


        private void CalcStats(List<Curve> curves, int valueCount, ref AnimationClipStatsPublic stats,
            ref CurveDetail curveDetail, List<BuildCurveKey> streamedKeys)
        {
            foreach (var curve in curves)
            {
                switch (curve.clipType)
                {
                    case ClipOptType.StreamedClip:
                        stats.streamCurves += valueCount;

                        foreach (var key in curve.keys)
                        {
                            for (var i = 0; i < valueCount; i++)
                            {
                                if (i == 0 && key.time == float.MinValue)
                                    continue;

                                var cache = new BuildCurveKey();
                                cache.time = key.time;
                                cache.curveIndex = curveDetail.StreamedClipCurveIndex + i;
                                streamedKeys.Add(cache);
                            }
                        }

                        curveDetail.StreamedClipCurveIndex += valueCount;
                        break;
                    case ClipOptType.DenseClip:
                        stats.denseCurves += valueCount;
                        curveDetail.DenseClipCurveIndex += valueCount;
                        break;
                    case ClipOptType.ConstantClip:
                        stats.constantCurves += valueCount;
                        curveDetail.ConstantCurveIndex += valueCount;
                        break;
                }
            }
        }

        private int CalcStreamedClipSize(List<BuildCurveKey> streamedKeys)
        {
            streamedKeys.Sort((lhs, rhs) =>
            {
                if (lhs.time != rhs.time)
                    return lhs.time < rhs.time ? -1 : 1;
                // for same time, Sort by curve index. This reduces cache trashing when sampling the clip
                else
                    return lhs.curveIndex < rhs.curveIndex ? -1 : 1;
            });

            var size = 0;
            var lastTime = float.MinValue;

            foreach (var curve in streamedKeys)
            {
                if (curve.time != lastTime)
                {
                    lastTime = curve.time;
                    size += sizeof(float); // Time
                    size += sizeof(Int32); // curve count
                }

                size += sizeof(Int32); // curve index
                size += sizeof(float) * 4; // coeff
            }

            return size;
        }

        private void BuildCurves()
        {
            var curveDatas = AnimationUtility.GetAllCurves(this.AnimationClip);
            foreach (var curveData in curveDatas)
            {
                AddToList(curveData);
            }

            BuildCurvesType(PositionCurves);
            BuildCurvesType(ScaleCurves);
            BuildCurvesType(RotationCurves);
        }

        private void AddToList(AnimationClipCurveData curveData)
        {
            //		string[] propertyDisplayName = { 
            //			"m_LocalPosition", "Position", 
            //			"m_LocalScale", "Scale", 
            //			"m_LocalRotation", "Rotation",
            //			"localEulerAnglesBaked", "Rotation",
            //			"localEulerAngles", "Rotation",
            //			"m_Materials.Array.data", "Material Reference",
            //		};

            var property = curveData.propertyName;

            if (property.StartsWith("m_LocalPosition"))
            {
                AddToList(curveData, PositionCurves);
            }
            else if (property.StartsWith("m_LocalScale"))
            {
                AddToList(curveData, ScaleCurves);
            }
            else if (property.StartsWith("m_LocalRotation"))
            {
                AddToList(curveData, RotationCurves);
            }
        }

        private void AddToList(AnimationClipCurveData curveData, List<Curve> curves)
        {
            var property = curveData.propertyName;

            if (property.EndsWith("x"))
            {
                curves.Add(new Curve());

                var item = curves.Last();
                item.path = curveData.path;
                item.preInfinity = (int) curveData.curve.preWrapMode;
                item.postInfinity = (int) curveData.curve.postWrapMode;

                AddToList(curveData, curves, 0);
            }
            else if (property.EndsWith("y"))
            {
                AddToList(curveData, curves, 1);
            }
            else if (property.EndsWith("z"))
            {
                AddToList(curveData, curves, 2);
            }
            else if (property.EndsWith("w"))
            {
                AddToList(curveData, curves, 3);
            }
        }

        private void AddToList(AnimationClipCurveData curveData, List<Curve> curves, int index)
        {
            var item = curves.Last();
            if (item.path != curveData.path)
                throw new FormatException();

            foreach (var key in curveData.curve.keys)
            {
                var keyData = item.keys.Find(x => Mathf.Abs(x.time - key.time) < float.Epsilon);
                if (keyData == null)
                {
                    keyData = new KeyframeContainer<Vector4>();
                    keyData.time = key.time;
                    item.keys.Add(keyData);
                }

                keyData.value[index] = key.value;
                keyData.inSlope[index] = key.inTangent;
                keyData.outSlope[index] = key.outTangent;
            }
        }

        private void BuildCurvesType(List<Curve> curves)
        {
            foreach (var curve in curves)
            {
                curve.keys.Sort((x, y) => x.time < y.time ? -1 : 1);

                if (IsConstantCurve(curve))
                    curve.clipType = ClipOptType.ConstantClip;
                else if (ENABLE_DENSE && IsDenseCurve(curve))
                    curve.clipType = ClipOptType.DenseClip;
                else
                    curve.clipType = ClipOptType.StreamedClip;
            }
        }

        private bool IsConstantCurve(Curve curve)
        {
            var first = curve.keys.First();
            foreach (var item in curve.keys)
            {
                if (!CompareApproximately(first.value, item.value))
                    return false;
                if (!CompareApproximately(first.inSlope, item.inSlope))
                    return false;
                if (!CompareApproximately(first.outSlope, item.outSlope))
                    return false;
            }

            return true;
        }

        private bool CompareApproximately(Vector4 left, Vector4 right)
        {
            var value = left - right;
            return value.sqrMagnitude < Vector4.kEpsilon * Vector4.kEpsilon;
        }

        private bool IsDenseCurve(Curve curve)
        {
            const float samplePerSec = 30.0f;
            const float sampleStep = 1.0f / samplePerSec;
            var begin = curve.keys.First().time;
            var end = curve.keys.Last().time;
            var diff = end - begin;
            if (diff * samplePerSec > curve.keys.Count * 8)
                return false;

            if (!IsFinite(curve.keys.First().inSlope) || !IsFinite(curve.keys.First().outSlope))
                return false;

            for (int i = 1; i < curve.keys.Count(); i++)
            {
                var previousKey = curve.keys[i - 1];
                var key = curve.keys[i];

                if (!IsFinite(key.inSlope) || !IsFinite(key.outSlope))
                    return false;

                if (IsTooDense(key, previousKey, sampleStep))
                    return false;
            }

            return true;
        }

        private static bool IsFinite(Vector4 vec)
        {
            return IsFinite(vec.x) && IsFinite(vec.y) && IsFinite(vec.z) && IsFinite(vec.w);
        }

        private static bool IsFinite(float value)
        {
            // Returns false if value is NaN or +/- infinity
            var intval = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
            return (intval & 0x7f800000) != 0x7f800000;
        }

        private static bool IsTooDense(KeyframeContainer<Vector4> key, KeyframeContainer<Vector4> previousKey,
            float sampleStep)
        {
            float delta = Mathf.Abs(key.time - previousKey.time);

            // epsilon is too small here, use a bigger threshold
            return (delta - sampleStep) < -1e-5f /*-std::numeric_limits<float>::epsilon()*/;
        }
    }
}