using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Saro.XAsset.Build
{
    public class BuildMethod
    {
        public int order;
        public string displayName;
        public bool required;
        public string tooltip;
        public bool selected = false;
        public Func<bool> callback;

        private static List<BuildMethod> s_BuildMethods;
        public static List<BuildMethod> BuildMethodCollection
        {
            get
            {
                if (s_BuildMethods == null)
                {
                    s_BuildMethods = GetBuildMethods();
                }
                return s_BuildMethods;
            }
        }

        private static List<BuildMethod> GetBuildMethods()
        {
            var allTypes = Utility.ReflectionUtility.GetSubClassTypesAllAssemblies(typeof(IBuildProcessor));

            var ret = new List<BuildMethod>();

            foreach (var type in allTypes)
            {
                var methods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<XAssetBuildMethodAttribute>();
                    if (attr != null)
                    {
                        var buildMethod = new BuildMethod()
                        {
                            order = attr.executeOrder,
                            displayName = attr.displayName,
                            tooltip = attr.tooltip,
                            required = attr.required,
                            selected = attr.required,
                        };

                        buildMethod.callback = () =>
                        {
                            if (method.ReturnType == typeof(bool))
                            {
                                var ret1 = (bool)method.Invoke(null, null);

                                Debug.Log($"Execute [{buildMethod.order}] [{buildMethod.displayName}] Successfull");

                                return ret1;
                            }
                            else
                            {
                                try
                                {
                                    method.Invoke(null, null);

                                    Debug.Log($"Execute [{buildMethod.order}] [{buildMethod.displayName}] Successfull");
                                }
                                catch (Exception e)
                                {
                                    Debug.LogException(e);
                                    return false;
                                }
                                finally
                                {
                                    EditorUtility.ClearProgressBar();
                                }

                                return true;
                            }
                        };

                        ret.Add(buildMethod);
                    }
                }
            }

            ret.Sort((a, b) => a.order.CompareTo(b.order));

            return ret;
        }
    }

    /// <summary>
    /// XAsset自动化打包流程方法
    /// <see cref="BuildMethods"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class XAssetBuildMethodAttribute : Attribute
    {
        /// <summary>
        /// 执行顺序
        /// </summary>
        public int executeOrder;
        /// <summary>
        /// 显示名称
        /// </summary>
        public string displayName;
        /// <summary>
        /// 是否必须被执行
        /// </summary>
        public bool required;
        /// <summary>
        /// 提示
        /// </summary>
        public string tooltip;

        public XAssetBuildMethodAttribute(int executeOrder, string displayName, bool required = false, string tooltip = null)
        {
            this.executeOrder = executeOrder;
            this.displayName = displayName;
            this.required = required;
            this.tooltip = tooltip;
        }
    }
}
