//using FlowSynx.Domain.Tenants.ValueObjects;

//namespace FlowSynx.Security;

//public class TenantAuthenticationFactory : ITenantAuthenticationFactory
//{
//    private readonly IServiceProvider _sp;

//    public TenantAuthenticationFactory(IServiceProvider sp)
//    {
//        _sp = sp;
//    }

//    public IAuthenticationProvider GetAuthenticator(AuthenticationMode mode)
//    {
//        return mode switch
//        {
//            AuthenticationMode.None => _sp.GetRequiredService<NoneAuthenticationProvider>(),
//            AuthenticationMode.Basic => _sp.GetRequiredService<BasicAuthenticationProvider>(),
//            AuthenticationMode.Jwt => _sp.GetRequiredService<JwtAuthenticationProvider>(),
//            _ => throw new InvalidOperationException("Unsupported authentication type")
//        };
//    }
//}