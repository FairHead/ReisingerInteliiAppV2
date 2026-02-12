namespace ReisingerIntelliApp_V4.Helpers
{
    public static class ServiceHelper
    {
        public static TService GetService<TService>() where TService : class
        {
            var serviceProvider = IPlatformApplication.Current?.Services;
            if (serviceProvider == null)
            {
                Console.WriteLine($"❌ ServiceHelper: IPlatformApplication.Current?.Services is null when resolving {typeof(TService).Name}");
                throw new InvalidOperationException($"Service provider not available when resolving {typeof(TService).Name}");
            }

            return serviceProvider.GetRequiredService<TService>();
        }
    }
}
