

[![License](https://img.shields.io/badge/license-view-orange)](https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md)

# Multifactor radius adapter

_Also, available in other languages: [Русский](README.ru.md)_

## What is Multifactor radius adapter?
**Multifactor radius adapter** is a RADIUS server for Linux. It allows you to quickly add multifactor authentication through RADIUS protocol to your VPN, VDI, RDP, and other resources.

The component is a part of <a href="https://multifactor.pro/" target="_blank">MultiFactor</a> 2FA hybrid solution.

* <a href="https://github.com/MultifactorLab/multifactor-radius-adapter" target="_blank">Source code</a>
* <a href="https://github.com/MultifactorLab/multifactor-radius-adapter/releases" target="_blank">Releases</a>

## Table of Contents

* [What is Multifactor radius adapter](#what-is-multifactor-radius-adapter)
* [Component Features](#component-features)
* [Use Cases](#use-cases)
* [Installation and configuration](#installation-and-configuration)
* [License](#license)

## RADIUS

Remote Authentication Dial-In User Service (RADIUS) &mdash; is a networking protocol primarily used for remote user authentication.

The protocol has been around for a long time and is supported by major network devices and services vendors.

## Component Features

Key features:

1. Receive authentication requests through the RADIUS protocol;
2. Verify the first authentication factor &mdash; user login and password in Active Directory (AD) or Network Policy Server (NPS);
3. Verify the second authentication factor on the user's secondary device (usually, mobile phone).

Additional features:

* Inline enrollment within VPN/VDI client;
* Conditional access based on the user's group membership in Active Directory;
* Activate second factor selectively based on the user's group membership in Active Directory;
* Use user's phone number from Active Directory profile for one-time SMS passcodes;
* Configure RADIUS response attributes based on user's Active Directory group membership;
* Proxy Network Policy Server requests and responses.

## Use Cases

Use Radius Adapter Component to implement 2FA in one of the following scenarios:

* Two-factor authentication for VPN devices [Cisco](https://multifactor.pro/docs/vpn/cisco-anyconnect-vpn-2fa/), [Fortigate](https://multifactor.pro/docs/vpn/fortigate-forticlient-vpn-2fa/), [CheckPoint](https://multifactor.pro/docs/vpn/checkpoint-remote-access-vpn-2fa/), Mikrotik, Huawei and others;
* Two-factor authentication for [Windows VPN with Routing and Remote Access Service (RRAS)](https://multifactor.pro/docs/windows-2fa-rras-vpn/);
* Two-factor authentication for [Microsoft Remote Desktop Gateway](https://multifactor.pro/docs/windows-2fa-remote-desktop-gateway/) ;
* Two-factor authentication for [VMware Horizon](https://multifactor.pro/docs/vmware-horizon-2fa/);
* [Citrix Gateway](https://multifactor.pro/docs/citrix-radius-2fa/) two-factor authentication;
* Apache Guacamole two-factor authentication;
* Two-factor authentication for Wi-Fi hotspots;

and many more...

## Installation and configuration
See <a href="https://multifactor.pro/docs/radius-adapter/linux/" target="_blank">knowledge base</a> for information about configuration, launch and an additional guidance.

## License

Please note, the <a href="https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md" target="_blank">license</a> does not entitle you to modify the source code of the Component or create derivative products based on it. The source code is provided as-is for evaluation purposes.