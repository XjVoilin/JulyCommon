namespace JulyCommon
{
    /// <summary>
    /// 框架相关的只读内容集中管理
    /// </summary>
    public static class Frameworkconst
    {
        public static readonly string FrameworkName = "JulyGF";

        // 日志标签
        public static readonly string TagCoreContext = "[CoreContext]";
        public static readonly string TagJulyGameEntry = "[JulyGameEntry]";
        public static readonly string TagDependencyContainer = "[DependencyContainer]";

        /// <summary>
        /// 标记为脏时,如果同时携带的信号级别是:Medium  那么如果当前总共被标记的个数的阈值超过了这个值,就会被保存
        /// </summary>
        public const int MediumDirtyCount = 3;
    }
}
