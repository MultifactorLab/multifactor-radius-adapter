[![License](https://img.shields.io/badge/license-view-orange)](https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md)

# multifactor-radius-adapter

_Also, available in other languages: [Русский](README.ru.md)_

**multifactor-radius-adapter** is a RADIUS server for Linux. It allows you to quickly add multifactor authentication through RADIUS protocol to your VPN, VDI, RDP, and other resources.

The component is a part of <a href="https://multifactor.pro/" target="_blank">MultiFactor</a> 2FA hybrid solution.

* <a href="https://github.com/MultifactorLab/multifactor-radius-adapter" target="_blank">Source code</a>
* <a href="https://github.com/MultifactorLab/multifactor-radius-adapter/releases" target="_blank">Build</a>

See documentation at <https://multifactor.pro/docs/radius-adapter/linux/> for additional guidance on integrating 2FA through RADIUS into your infrastructure.

Windows version of the component is available in our [MultiFactor.Radius.Adapter](https://github.com/MultifactorLab/MultiFactor.Radius.Adapter) repository.

## Table of Contents

* [Background](#background)
  * [Component Features](#component-features)
* [Prerequisites](#prerequisites)
* [Installation](#installation)
  * [Dependencies Installation](#dependencies-installation)
    * [CentOS 7](#centos-7)
    * [CentOS 8](#centos-8)
    * [Ubuntu 18.04](#ubuntu-1804)
    * [Debian 10](#debian-10)
  * [Component Installation](#component-installation)
* [Configuration](#configuration)
  * [General Parameters](#general-parameters)
  * [Active Directory Connection Parameters](#active-directory-connection-parameters)
  * [External RADIUS Server Connection](#external-radius-server-connection)
  * [Optional RADIUS Attributes](#optional-radius-attributes)
  * [Second factor verification parameters](#second-factor-verification-parameters)
  * [2FA before 1FA](#second-factor-authentication-before-first-factor-authentication)
  * [Customize logging](#logging)
  * [Environment variables](#configuring-the-adapter-using-environment-variables)
* [Start-Up](#start-up)
* [Logs](#logs)
* [Limitations of Active Directory Integration](#limitations-of-active-directory-integration)
* [Use Cases](#use-cases)
* [License](#license)

## Background

Remote Authentication Dial-In User Service (RADIUS) &mdash; is a networking protocol primarily used for remote user authentication.

The protocol has been around for a long time and is supported by major network devices and services vendors.

### Component Features

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

## Prerequisites

* Component is installed on a Linux server, tested on CentOS, Ubuntu, Debian;
* Minimum server requirements: 1 CPU, 2 GB RAM, 8 GB HDD (to run the OS and adapter for 100 simultaneous connections &mdash; approximately 1500 users);
* Port 1812 (UDP) must be open on the server to receive requests from Radius clients;
* The server with the component installed needs access to ```api.multifactor.ru``` via TCP port 443 (TLS) directly or via HTTP proxy;
* To interact with Active Directory, the component needs access to the domain server via TCP port 389 (LDAP scheme) or 636 (LDAPS scheme);
* To interact with the Network Policy Server, the component needs access to NPS via UDP port 1812.

## Installation

### Dependencies Installation

The component uses the .NET 6 runtime environment, which is free, open-source, developed by Microsoft and the open-source community. The runtime environment does not impose any restrictions on its use.

To install, run the commands:

#### CentOS 7

```shell
sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
sudo yum install aspnetcore-runtime-6.0
```

<a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-centos" target="_blank">https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-centos</a>

#### CentOS 8

> ⚠️ **Warning**  
> CentOS Linux 8 reached an early End Of Life (EOL) on December 31st, 2021.  
> For more information, see the official <a href="https://www.centos.org/centos-linux-eol/" target="_blank">CentOS Linux EOL page</a>.
> Because of this, .NET isn't supported on CentOS Linux 8.

For more information see <a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-centos" target="_blank">this page</a>.  
See also: <a href="https://learn.microsoft.com/ru-ru/dotnet/core/install/linux-rhel#supported-distributions">install the .NET on CentOS Stream</a>.

#### Ubuntu 18.04

```shell
$ wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
$ sudo dpkg -i packages-microsoft-prod.deb

$ sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y aspnetcore-runtime-6.0
```

<a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-ubuntu" target="_blank">https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-ubuntu</a>

#### Debian 10

```shell
$ wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
$ sudo dpkg -i packages-microsoft-prod.deb

$ sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y aspnetcore-runtime-6.0
```

<a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-debian" target="_blank">https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-debian</a>

### Component Installation

Create a folder, download and unzip the current version of the component from <a href="https://github.com/MultifactorLab/multifactor-radius-adapter/releases/" target="_blank">GitHub</a>:

```shell
sudo mkdir /opt/multifactor /opt/multifactor/radius /opt/multifactor/radius/logs
sudo wget https://github.com/MultifactorLab/multifactor-radius-adapter/releases/latest/download/release_linux_x64.zip
sudo unzip release_linux_x64.zip -d /opt/multifactor/radius
```

Create a system user mfa and give it rights to the application:

```shell
sudo useradd -r mfa
sudo chown -R mfa: /opt/multifactor/radius/
sudo chmod -R 700 /opt/multifactor/radius/
```

Create a service

```shell
sudo vi /etc/systemd/system/multifactor-radius.service
```

```shell
[Unit]
Description=Multifactor Radius Adapter

[Service]
WorkingDirectory=/opt/multifactor/radius/
ExecStart=/usr/bin/dotnet /opt/multifactor/radius/multifactor-radius-adapter.dll
Restart=always
# Restart service after 10 seconds if the service crashes:
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=multifactor-radius
User=mfa
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
 
# How many seconds to wait for the app to shut down after it receives the initial interrupt signal. 
# If the app doesn't shut down in this period, SIGKILL is issued to terminate the app. 
# The default timeout for most distributions is 90 seconds.
TimeoutStopSec=30

[Install]
WantedBy=multi-user.target
```

Enable autorun:

```shell
sudo systemctl enable multifactor-radius
```

## Configuration

The component's parameters are stored in ```/opt/multifactor/radius/multifactor-radius-adapter.dll.config``` in XML format.

### General Parameters

```xml
<appSettings>
  <!-- The address and port (UDP) on which the adapter will receive authentication requests from clients -->
  <!-- If you specify 0.0.0.0, then the adapter will listen on all network interfaces -->
  <add key="adapter-server-endpoint" value="0.0.0.0:1812"/>

  <!-- Shared secret to authenticate RADIUS clients -->
  <add key="radius-shared-secret" value=""/>

  <!-- How to check the first factor: Active Directory, RADIUS or None (do not check) -->
  <add key="first-factor-authentication-source" value="ActiveDirectory"/>

  <!-- Multifactor API address -->
  <add key="multifactor-api-url" value="https://api.multifactor.ru"/>
  <!--Timeout for requests in the Multifactor API, the minimum value is 65 seconds-->
  <add key="multifactor-api-timeout" value="00:01:05"/>
  <!-- NAS-Identifier parameter to connect to the Multifactor API (found in user profile) -->
  <add key="multifactor-nas-identifier" value=""/>
  <!-- Shared Secret parameter to connect to the Multifactor API (found in user profile) -->
  <add key="multifactor-shared-secret" value=""/>

  <!-- Use this option to access the Multifactor API via HTTP proxy (optional)-->
  <!--add key="multifactor-api-proxy" value="http://proxy:3128"/-->

  <!-- Logging level: 'Debug', 'Info', 'Warn', 'Error' -->
  <add key="logging-level" value="Debug"/>
</appSettings>
```

### Active Directory Connection Parameters

To check the first factor in the Active Directory domain, the following parameters apply:

```xml
<appSettings>
  <!--ActiveDirectory authentication settings: for example domain.local on host 10.0.0.4 -->
  <add key="active-directory-domain" value="ldaps://10.0.0.4/DC=domain,DC=local"/>

  <!--Give access to users from specified group only (not checked if setting is removed)-->
  <add key="active-directory-group" value="VPN Users"/>
  <!--Require the second factor for users from a specified group only (second factor is required for users if the setting is removed)-->
  <add key="active-directory-2fa-group" value="2FA Users"/>
  <!--Use your users' phone numbers listed in Active Directory to send one-time SMS codes (not used if settings are removed)-->
  <!--add key="use-active-directory-user-phone" value="true"/-->
  <!--add key="use-active-directory-mobile-user-phone" value="true"/-->
</appSettings>
```

When the ```use-active-directory-user-phone``` option is enabled, the component will use the phone recorded in the General tab. The format of the phone can be anything.

<img src="https://multifactor.pro/img/radius-adapter/ra-ad-phone-source.png" alt="AD phone" width="300">

When the ```use-active-directory-mobile-user-phone``` option is enabled, the component will use the phone recorded in the Telephones tab in the Mobile field. The format of the phone can also be any format.

<img src="https://multifactor.pro/img/radius-adapter/ra-ad-mobile-phone-source.png" alt="AD mobile phone" width="300">

### External RADIUS Server Connection

To check the first factor in RADIUS, for example in Network Policy Server, the following parameters are applicable:

```xml
<appSettings>
  <!--Address (UDP) from which the adapter will connect to the server -->
  <add key="adapter-client-endpoint" value="192.168.0.1"/>
  <!--Server address and port (UDP) -->
  <add key="nps-server-endpoint" value="192.168.0.10:1812"/>
</appSettings>
```

### Optional RADIUS Attributes

You can specify attributes the component will pass further upon successful authentication, including verification that the user is a member of a security group.

```xml
<RadiusReply>
    <Attributes>
        <!--This is an example, any attributes can be used-->
        <add name="Class" value="Super" />
        <add name="Fortinet-Group-Name" value="Users" when="UserGroup=VPN Users"/>
        <add name="Fortinet-Group-Name" value="Admins" when="UserGroup=VPN Admins"/>
    </Attributes>
</RadiusReply>
```

### Second factor verification parameters

The following parameters will help you set up access to the MULTIFACTOR API when checking the second factor:

```xml
<appSettings>
  <!-- Use the specified attribute as the user identity when checking the second factor-->
  <add key="use-attribute-as-identity" value="mail"/>
  <!-- Skip repeated authentications without requesting the second factor for 1 hour, 20 minutes, 10 seconds (caching is disabled if you remove the setting) -->
  <add key="authentication-cache-lifetime" value="01:20:10" />
  <!-- If the API is unavailable, skip the MULTIFACTOR without checking (by default), or deny access (false) -->
  <add key="bypass-second-factor-when-api-unreachable" value="true"/>
  <!-- Automatically assign MULTIFACTOR group membership to registering users -->
  <add key="sign-up-groups" value="group1;Group name 2"/>
</appSettings>
```

### Second Factor Authentication before First Factor Authentication

The Adapter now supports new mode: Second Factor Authentication before First Factor Authentication.
If this mode is enabled, the user will have to confirm the second factor before he can proceed to confirm the first (login/password).
All current features such as BYPASS and INLINE ENROLLMENT are available in the new mode as well.

> Note: The Second Factor Authentication before First Factor Authentication mode is not available for **Winlogon** and **RDGW** resources.

All available methods - push, telegram, otp - specifies the preferred method for the current user during the authentication session on the Multifactor Cloud side. This means that the specified method will be preferred. But if this method is not available, the next one will be used according to priority.

In **otp** mode, the user must enter the OTP code in the `User-Password` attribute along with the password. If no password is required, the user only needs to enter the OTP code.  
Examples of `User-Password` attribute content:

- password + otp: mypassword123456
- otp only: 123456

#### Configuration

You can activate this mode by adding the following option to the client config:  
`<add key="pre-authentication-method" value="METHOD"/>`
Allowed **METHOD** values: none (by default), push, telegram, otp.

If the mode is enabled (push, telegram, otp) it is necessary to add invalid credential delay settings in root or client level config:
`<add key="invalid-credential-delay" value="DELAY"/>`  
The minimal value of **DELAY** must be 2.

#### Configuration examples

```xml
<appSettings>
  <!-- feature disabled -->
  <add key="pre-authentication-method" value="none"/>
  <add key="invalid-credential-delay" value="0"/>
  
  <!-- push -->
  <add key="pre-authentication-method" value="push"/>
  <add key="invalid-credential-delay" value="2"/>
  
  <!-- telegram -->
  <add key="pre-authentication-method" value="telegram"/>
  <add key="invalid-credential-delay" value="3-5"/>
  
  <!-- otp -->
  <add key="pre-authentication-method" value="otp"/>
  <add key="invalid-credential-delay" value="4"/>
</appSettings>
```

### Logging

There are such options to customize logging:

```xml
<appSettings>
  <!--Allows you to customize the template of logs which get into the system log -->
  <add key="console-log-output-template" value="outputTemplate"/>
  <!--Allows you to customize the logs’ template which get into the file -->
  <add key="file-log-output-template" value="outputTemplate"/>
</appSettings>
```

As ```outputTemplate``` also acts text template which shows the logging system how the message should be formatted. For example

 ```sh
[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}
[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception} 
```

For more information [see this page.](https://github.com/serilog/serilog/wiki/Formatting-Output)

Moreover, logging can be provided in json:

```xml
<appSettings>
  <add key="logging-format" value="format"/>
</appSettings>
```

Keep in mind that ```console-log-output-template``` and ```file-log-output-template``` settings are not applicable for the JSON log format, but it's possible to choose from predefined formats. Here are possible values of the ```format``` parametr (register is not case-sensitive).

* ```Json``` or ```JsonUtc```. Compact logging, times in UTC.

   ```json
   {"@t":"2016-06-07T03:44:57.8532799Z","@m":"Hello, \"nblumhardt\"","@i":"7a8b9c0d","User":"nblumhardt"}
   ```

* ```JsonTz```. Compact logging, differs from ```JsonUtc``` by the time format. In this kind of format the local time with time zone is indicated.

  ```Json
   {"@t":"2023-11-23 17:16:29.919 +03:00","@m":"Hello, \"nblumhardt\"","@i":"7a8b9c0d","User":"nblumhardt"}
   ```

* ```Ecs```. Ecs formats logs according to elastic common schema.

   ```json
   {
     "@timestamp": "2019-11-22T14:59:02.5903135+11:00",
     "log.level": "Information",
     "message": "Log message",
     "ecs": {
       "version": "1.4.0"
     },
     "event": {
       "severity": 0,
       "timezone": "AUS Eastern Standard Time",
       "created": "2019-11-22T14:59:02.5903135+11:00"
     },
     "log": {
       "logger": "Elastic.CommonSchema.Serilog"
     },
     "process": {
       "thread": {
         "id": 1
       },
       "executable": "System.Threading.ExecutionContext"
     }
   }
   ```
### Configuring the Adapter using Environment Variables

There is another way to configure the Adapter - by setting environment variables.  
This approach has a number of advantages:
- independence from configuration files: solves the problem of possible overwriting of files;
- easier containerization: just set a set of environment variables inside the container;
- increased security: sensitive data can be transferred via variables without using the file system.

When launched, the Adapter reads the configuration from the `multifactor-radius-adapter.dll.config` file, as well as from the `*.config` files located in the **/clients** folder.  
After this, the adapter receives values from the environment and "overlays" them on top of the settings read from the settings files.  
Thus, it turns out that values from environment variables overload values from settings files.  
By the way, you don’t have to use settings files at all (leave them with default values or delete the files): any settings can be described through environment variables.

##### Examples

Syntax:
```shell
# Linux
export VAR=VALUE

# Windows (PowerShell)
$Env:VAR = VALUE
```
**VAR** - environment variable name, **VALUE** - variable value.  
The `export` directive is needed to set the specified variable not only for the current shell, but also for all processes launched from this shell.

To pass settings to the adapter via environment variables, you must specify the name correctly.  
To transfer the setting to the main config (multifactor-radius-adapter.dll.config), the variable name should look like this:
```shell
export RAD_APPSETTINGS__FirstFactorAuthenticationSource=ActiveDirectory
```
**RAD_** - prefix.  
**APPSETTINGS** - section inside the configuration file.  
**FirstFactorAuthenticationSource** - name of a setting.  
**__** - nesting separator.  
Case of variable name is **not important**.

> Note: if the name of the configuration file contains whitespace characters, when forming the name for the environment variable, these spaces must be ignored: `my rad` -> `myrad`.

Alternative way:
```xml
<appsettings>
  <add key="first-factor-authentication-source" value="ActiveDirectory" />
</appsettings>
```

More complex example:
```shell
export RAD_RADIUSREPLY__ATTRIBUTES__ADD__0__NAME=Class
export RAD_RADIUSREPLY__ATTRIBUTES__ADD__0__VALUE=users1
```
0 - an index (number) of element.

Alternative way:
```xml
<RadiusReply>
  <Attributes>
    <add name="Class" value="users1" />
  </Attributes>
</RadiusReply>
```

If you need to pass setting to the client configuration, you must just insert configuration name after **RAD_** prefix. For example, for the configuration that is in the `my-rad.config` file, the variable name would look like this:  
```shell
export RAD_my-rad_APPSETTINGS__FirstFactorAuthenticationSource=ActiveDirectory
```


## Start-Up

After configuring the configuration, run the component:

```shell
sudo systemctl start multifactor-radius
```

You can check the status with the command:

```shell
sudo systemctl status multifactor-radius
```

## Logs

The logs of the component are located in the ``/opt/multifactor/radius/logs`` folder as well as in the system log.

## Limitations of Active Directory Integration

* The Linux version of the Adapter _can't yet_ handle multiple domains with trust established between them;
* A simple user's password authentication is used with Active Directory. We strongly recommend using the LDAPS scheme to encrypt traffic between the adapter and the domain (AD server must have a certificate installed, including a self-signed one).

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

## License

Please note, the <a href="https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md" target="_blank">license</a> does not entitle you to modify the source code of the Component or create derivative products based on it. The source code is provided as-is for evaluation purposes.
