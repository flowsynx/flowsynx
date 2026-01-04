//using FlowSynx.Domain.TenantSecretConfigs.Security;

//namespace FlowSynx.Domain.Tenants.ValueObjects;

//public sealed record SecurityConfiguration
//{
//    public AuthenticationConfiguration Authentication { get; set; } = new();

//    public static SecurityConfiguration Create()
//    {
//        return new SecurityConfiguration
//        {
//            Authentication = new AuthenticationConfiguration
//            {
//                Mode = AuthenticationMode.None,
//                Basic = new BasicConfiguration
//                {
//                    Users = new List<BasicAuthenticationConfiguration>()
//                    {
//                        new BasicAuthenticationConfiguration
//                        {
//                            Id = "0960a93d-e42b-4987-bc07-7bda806a21c7",
//                            Name = "admin",
//                            Password = "admin",
//                            Roles = new List<string> { "admin" }
//                        }
//                    }
//                },
//                Jwt = new JwtAuthenticationsConfiguration
//                {
//                    Name = string.Empty,
//                    Authority = string.Empty,
//                    Audience = string.Empty,
//                    Issuer = string.Empty,
//                    Secret = string.Empty,
//                    RequireHttps = false,
//                    RoleClaimNames = new List<string> { "role", "roles" }
//                }
//            }
//        };
//    }
//}