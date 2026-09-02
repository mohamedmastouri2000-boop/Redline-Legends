namespace RedlineLegends.Core
{
    /// <summary>
    /// The single static entry point to the service container.
    /// Scene-instantiated MonoBehaviours (UI screens, race scene objects) cannot receive constructor
    /// injection, so they resolve their dependencies here in Awake/Start. Pure C# classes should
    /// receive dependencies through constructors instead of calling this.
    /// </summary>
    public static class Services
    {
        public static ServiceContainer Container { get; private set; }
        public static bool IsReady => Container != null;

        public static T Get<T>() where T : class => Container.Resolve<T>();

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Container == null)
            {
                service = null;
                return false;
            }
            return Container.TryResolve(out service);
        }

        public static void Install(ServiceContainer container) => Container = container;

        public static void Uninstall()
        {
            Container?.Clear();
            Container = null;
        }
    }
}
