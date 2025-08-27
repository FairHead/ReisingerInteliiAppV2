namespace ReisingerIntelliApp_V4.Helpers
{
    public static class ServiceHelper
    {
        public static TService GetService<TService>() where TService : class
        {
            var serviceProvider = IPlatformApplication.Current?.Services
                ?? throw new InvalidOperationException("Service provider not available");

            return serviceProvider.GetRequiredService<TService>();
        }
    }
}
