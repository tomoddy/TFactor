# TFactor

A local two-factor authentication (TOTP) app for Windows, built with WPF/.NET. Your secrets stay on your machine, encrypted with Windows DPAPI, and unlocked with Windows Hello.

## Features

- Manual account entry, or import via QR code screenshot - either a single `otpauth://` code or a Google Authenticator "Transfer accounts" bulk export (including exports split across multiple QR codes).
- Live, auto-refreshing TOTP codes with a countdown that changes color as it runs low.
- Edit an account's issuer/label, or remove it, at any time.
- Secrets are encrypted at rest with Windows DPAPI, tied to your Windows login.
- App access is gated behind Windows Hello (face, fingerprint, or PIN).
- Click a code to copy it to the clipboard.

## Requirements

- Windows 10 (1809/build 17763) or later
- .NET 10 SDK, to build and run

## Usage

```
dotnet run --project TFactor
```

Or open `TFactor.slnx` in Visual Studio and run from there.

## Tech

WPF (.NET 10), [ZXing.Net](https://github.com/micjahn/ZXing.Net) for QR decoding, and a hand-rolled TOTP/HOTP implementation (RFC 6238 / RFC 4226).

## Credits

Icons by gravisio - Flaticon
https://www.flaticon.com/free-icons
