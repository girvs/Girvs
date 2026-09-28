# OAuth 2.0 认证服务器配置迁移设计

## 背景

`Girvs.AuthorizePermission` 当前以 `IdentityServer4` 命名外部 JWT Bearer 认证配置。该实现本质上依赖 ASP.NET Core `AddJwtBearer`，可对接提供标准 OAuth 2.0 / OpenID Connect 元数据的认证服务器，不应绑定到已特定化的 IdentityServer4 产品名称。

本次执行破坏性迁移，直接删除全部 `IdentityServer4` 专用公开命名，不提供兼容别名。

## 公开 API 与配置模型

统一使用以下名称：

- `AuthorizationModel.OAuth2`，保留原枚举位值 `2`。
- `AuthorizeConfig.OAuth2Config`。
- `OAuth2Config`。
- `GirvsAuthenticationScheme.GirvsOAuth2`，Scheme 字符串值为 `GirvsOAuth2`。

`OAuth2Config` 使用 ASP.NET Core JWT Bearer 的标准术语：

- `Authority`：认证服务器基址，替代 `ServerHost`。
- `Audience`：受保护 API 的资源标识，替代 `ApiResourceName`。
- `RequireHttpsMetadata`：是否要求通过 HTTPS 获取元数据，替代 `UseHttps`。
- `ValidateIssuerSigningKey`、`ValidateIssuer`、`ValidateAudience`：保留现有校验开关及默认值，避免除命名迁移以外的隐式行为变化。

删除未被认证注册逻辑使用的 `ApiSecret`。

## 认证注册行为

当 `AuthorizationModel` 包含 `OAuth2` 标志时，通过 `AddJwtBearer` 注册 `GirvsOAuth2` Scheme：

1. 将 `OAuth2Config.Authority` 映射到 `JwtBearerOptions.Authority`。
2. 将 `OAuth2Config.Audience` 映射到 `JwtBearerOptions.Audience`。
3. 将 `OAuth2Config.RequireHttpsMetadata` 映射到 `JwtBearerOptions.RequireHttpsMetadata`。
4. 将三个验证开关映射到 `TokenValidationParameters`。
5. 删除自定义 `SignatureValidator`。令牌解析、签名算法和签名密钥验证交由标准 `JwtBearerHandler` 与认证服务器元数据完成，避免绕过框架默认签名处理。

现有本地 `Jwt` 与 `JwtWebFront` 模式不变，Flags 组合注册行为不变。

## 破坏性变更范围

下列标识符及对应配置键将被删除：

- `AuthorizationModel.IdentityServer4`
- `AuthorizeConfig.IdentityServer4Config`
- `IdentityServer4Config`
- `GirvsAuthenticationScheme.GirvsIdentityServer4`
- `IdentityServer4Config:ServerHost`
- `IdentityServer4Config:ApiResourceName`
- `IdentityServer4Config:UseHttps`
- `IdentityServer4Config:ApiSecret`

使用方必须同步更新源码中的认证 Scheme、枚举值名称，以及 `ModuleConfigurations:AuthorizeConfig` 下的配置键。

## 测试与验收

新增 `Girvs.AuthorizePermission` 认证注册测试，至少验证：

- 仅启用 `OAuth2` 时注册 `GirvsOAuth2` Scheme。
- `Authority`、`Audience`、`RequireHttpsMetadata` 正确传入 `JwtBearerOptions`。
- 三个 Token 校验开关正确传入 `TokenValidationParameters`。
- `SignatureValidator` 未被自定义。
- `OAuth2` 与其他 Flags 组合时能够共同注册。

最终执行对应测试项目、模块构建及全仓文本扫描，确认生产代码、测试和示例配置中不再存在 `IdentityServer4` 专用命名。

## 非目标

- 不实现 OAuth 2.0 授权码、客户端凭据等令牌获取流程；本模块仍是资源服务器侧的 Bearer Token 验证组件。
- 不调整本地 `Jwt`、`JwtWebFront` 的签发和验证策略。
- 不提供旧配置键的自动迁移或运行时兼容。
