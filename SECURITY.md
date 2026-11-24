# Security Policy

## Supported Versions

We actively support the following versions of Klacks with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

We take the security of Klacks seriously. If you believe you have found a security vulnerability, please report it to us responsibly.

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please use the **GitHub Security Advisory** feature of this repository:
1. Go to the "Security" tab.
2. Click on "Advisories".
3. Click "Report a vulnerability".

Alternatively, you can contact the maintainers directly if a security contact is provided in the repository's metadata.

### Our Response Process

1. **Acknowledgment:** We will acknowledge receipt of your report within 48 hours.
2. **Investigation:** We will investigate the issue and determine its impact.
3. **Resolution:** If a vulnerability is confirmed, we will work on a fix.
4. **Disclosure:** We will coordinate the release of a fix and, if necessary, a public advisory.

## Security Best Practices for Contributors

- Never commit secrets (API keys, passwords, certificates) to the repository.
- Use `appsettings.Production.json.template` for production configuration.
- Ensure all inputs are validated using the established `FluentValidation` patterns.
- Keep dependencies updated via Dependabot alerts.
