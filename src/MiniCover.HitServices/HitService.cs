namespace MiniCover.HitServices
{
    public static class HitService
    {
        public static IHitContextStorage HitContextStorage { get; set; } = new FileHitContextStorage();

        public static MethodScope EnterMethod(
            string hitsPath,
            string assemblyName,
            string className,
            string methodName)
        {
            return new MethodScope(hitsPath, assemblyName, className, methodName);
        }
    }
}
