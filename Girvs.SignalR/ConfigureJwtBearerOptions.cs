using System.Threading.Tasks;

namespace Girvs.SignalR;

public class ConfigureJwtBearerOptions : IPostConfigureOptions<JwtBearerOptions>
{
    public void PostConfigure(string name, JwtBearerOptions options)
    {
        var originalOnMessageReceived = options.Events?.OnMessageReceived;

        // 不整体替换 Events，避免清空业务在 AddJwtBearer 中配置的
        // OnTokenValidated/OnChallenge 等安全回调。
        options.Events ??= new JwtBearerEvents();

        options.Events.OnMessageReceived = async context =>
        {
            if (originalOnMessageReceived is not null)
                await originalOnMessageReceived(context);

            if (string.IsNullOrEmpty(context.Token))
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
            }
        };
    }
}