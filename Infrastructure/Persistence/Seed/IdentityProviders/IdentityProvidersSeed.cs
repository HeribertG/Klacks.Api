using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed.IdentityProviders;

public static class IdentityProvidersSeed
{
    public static void SeedData(MigrationBuilder migrationBuilder)
    {
        var now = DateTime.UtcNow;

        migrationBuilder.Sql($@"
            INSERT INTO identity_providers (
                id, name, type, is_enabled, sort_order,
                use_for_authentication, use_for_client_import,
                host, port, use_ssl, base_dn, bind_dn, bind_password, user_filter,
                client_id, client_secret, authorization_url, token_url, user_info_url, scopes,
                create_time, update_time, is_deleted
            ) VALUES
            -- Forumsys LDAP Test (public test server)
            (
                'fc0b8e9c-4694-40c1-a10e-fdb66a10b26e',
                'Forumsys LDAP Test',
                0, -- LDAP
                true,
                10,
                true,
                true,
                'ldap.forumsys.com',
                389,
                false,
                'dc=example,dc=com',
                'cn=read-only-admin,dc=example,dc=com',
                'password',
                '(objectClass=person)',
                NULL, NULL, NULL, NULL, NULL, NULL,
                '{now:yyyy-MM-dd HH:mm:ss}',
                '{now:yyyy-MM-dd HH:mm:ss}',
                false
            ),
            -- Synology SSO (OIDC) - Update host/client_id/client_secret for your environment
            (
                '77f92a22-9817-4d54-9371-9e3807a4414c',
                'Synology SSO (OIDC)',
                3, -- OpenIdConnect
                true,
                10,
                true,
                false,
                '192.168.1.163',
                NULL,
                true,
                NULL, NULL, NULL, NULL,
                'ae3cdef3fa51abec90f01eac4ae353ea',
                'YRI2S5Rs9R8Ve3PHssZaOIpMycYJmoZU',
                'https://192.168.1.163/sso/webman/sso/SSOOauth.cgi',
                'https://192.168.1.163/sso/webman/sso/SSOAccessToken.cgi',
                'https://192.168.1.163/sso/webman/sso/SSOUserInfo.cgi',
                'openid email groups',
                '{now:yyyy-MM-dd HH:mm:ss}',
                '{now:yyyy-MM-dd HH:mm:ss}',
                false
            )
            ON CONFLICT (id) DO NOTHING;
        ");
    }
}
