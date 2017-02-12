# Apple Pay JS Example Integration

This repository contains a sample implementation of [Apple Pay JS](https://developer.apple.com/reference/applepayjs/) using ASP.NET Core written in C# and JavaScript.

## Overview

Apple Pay JS is a way of accepting Apple Pay in websites using Safari in either iOS 10 and macOS for users who have a TouchID compatible device.

This example integration shows a minimal sample of how to integrate Apple Pay into a webpage to obtain an Apple Pay token that can be used to capture funds for a user for goods and services.

The key components to look at for the implementation are:

  * ```src\ApplePayJS\Views\Home\Index.cshtml``` - The view that renders the Apple Pay button.
  * ```src\ApplePayJS\wwwroot\css\site.css``` - The CSS used to style the Apple Pay button.
  * ```src\ApplePayJS\wwwroot\js\site.js``` - The JavaScript that performs the majority of the Apple Pay functionality.
  * ```src\ApplePayJS\Controllers\HomeController.cs``` - The controller that performs the POST to the Apple Pay service to verify a merchant session.

## Setup

To setup the repository to run the sample, perform the steps below:

  1. Install the [.NET Core 1.0.0 RC4 SDK](https://github.com/dotnet/core/blob/master/release-notes/rc4-download.md).
  1. Fork this repository.
  1. Clone the repository from your fork to your local machine: ```git clone https://github.com/{username}/ApplePayJSSample.git```
  1. [Follow the steps](https://developer.apple.com/reference/applepayjs#2193397) to obtain your Apple Pay Merchant ID, Payment Processing Certificate, Domain Verification file and Merchant Identity Certificate if you do not already have them.
  1. Place the Domain Verification file (```apple-developer-merchantid-domain-association```) in the ```src\ApplePayJS\wwwroot\.well-known``` folder.
  1. Generate a ```.pfx``` file from your Merchant Identity Certificate (```merchant_id.cer```, which is the public key) and the Certificate Signing Request (```your_file_name.csr```, which is the private key) that you used to generate it.
  1. Either add your ```.pfx``` file to the root of the application in ```src\ApplePayJS``` (but **not** in the ```wwwroot``` folder) or install it into the local certificate store.
  1. Update the Apple touch icon (```src\ApplePayJS\wwwroot\apple-touch-icon.png```) and favicon (```src\ApplePayJS\wwwroot\favicon.ico```) to your own designs.
  1. Configure the following settings as appropriate in either your environment variables or in ```src\ApplePayJS\appsettings.json```:
    * ```ApplePay:StoreName```
    * ```ApplePay:UseCertificateStore```
    * ```ApplePay:MerchantCertificateThumbprint``` or ```ApplePay:MerchantCertificateFileName```
  1. Configure the following setting in either your environment variables, your [user secrets](https://docs.asp.net/en/latest/security/app-secrets.html#secret-manager) or in ```src\ApplePayJS\appsettings.json``` (**not recommended**) if loading the ```.pfx``` file from disk (i.e. ```ApplePay:UseCertificateStore=false```):
    * ```ApplePay:MerchantCertificatePassword```
  1. Deploy the application to the hosting environment for the domain where you wish to use Apple Pay JS.
  1. Verify the domain in the [Apple Developer Portal](https://developer.apple.com/account/).

You should now be able to perform Apple Pay JS transactions on the deployed application.

## Local Debugging

You should be able to debug the application in [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio 2017 RC](https://www.visualstudio.com/downloads/).

## Feedback

Any feedback or issues can be added to the [issues](https://github.com/justeat/ApplePayJSSample/issues) for this project in GitHub.

## License

This project is licensed under the [Apache 2.0](https://github.com/justeat/ApplePayJSSample/blob/master/LICENSE) license.

## External Links

  * [Apple Pay JS](https://developer.apple.com/reference/applepayjs)
  * [Apple Developer Portal](https://developer.apple.com/account/)
