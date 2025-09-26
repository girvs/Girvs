namespace Example.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GirvsHostBuilderManager
                .CreateGrivsHostBuilder<Startup>(args)
                .UseDefaultServiceProvider(options =>
                {
                    //we don't validate the scopes, since at the app start and the initial configuration we need
                    //to resolve some services (registered as "scoped") through the root container
                    options.ValidateScopes = false;
                    options.ValidateOnBuild = true;
                })
                .Build()
                .Run();
        }
    }
}
