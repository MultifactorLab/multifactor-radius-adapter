[![License](https://img.shields.io/badge/license-view-orange)](https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md)

# multifactor-radius-adapter

_Also available in other languages: [English](README.md)_

MultiFactor Radius Adapter &mdash; программный компонент, RADIUS сервер для Linux.

Компонент является частью гибридного 2FA решения сервиса <a href="https://multifactor.ru/" target="_blank">MultiFactor</a>.

* <a href="https://github.com/MultifactorLab/multifactor-radius-adapter" target="_blank">Исходный код</a>
* <a href="https://github.com/MultifactorLab/multifactor-radius-adapter/releases" target="_blank">Сборка</a>

Дополнительные инструкции по интеграции 2FA через RADIUS в вашу инфраструктуру см. в документации по адресу <https://multifactor.pro/docs/radius-adapter/linux/>.

Windows-версия компонента также доступна в репозитории [MultiFactor.Radius.Adapter](https://github.com/MultifactorLab/MultiFactor.Radius.Adapter).

## Содержание

* [Общие сведения](#общие-сведения)
  * [Функции компонента](#функции-компонента)
* [Требования для установки компонента](#требования-для-установки-компонента)
* [Установка](#установка)
  * [Установка зависимостей](#установка-зависимостей)
    * [CentOS 7](#centos-7)
    * [CentOS 8](#centos-8)
    * [Ubuntu 18.04](#ubuntu-1804)
    * [Debian 10](#debian-10)
  * [Установка компонента](#установка-компонента)
* [Конфигурация](#конфигурация)
  * [Общие параметры](#общие-параметры)
  * [Параметры подключения к Active Directory](#параметры-подключения-к-active-directory)
  * [Параметры подключения к внешнему RADIUS серверу](#параметры-подключения-к-внешнему-radius-серверу)
  * [Дополнительные RADIUS атрибуты](#дополнительные-radius-атрибуты)
  * [Параметры проверки второго фактора](#параметры-проверки-второго-фактора)
  * [Настройка журналирования](#журналирование)
* [Запуск компонента](#запуск-компонента)
* [Журналы](#журналы)
* [Ограничения работы с Active Directory](ограничения-работы-с-active-directory)
* [Сценарии использования](#сценарии-использования)
* [Лицензия](#лицензия)

## Общие сведения

Что такое RADIUS?

Remote Authentication Dial-In User Service (RADIUS) &mdash; сетевой протокол для удаленной аутентификации пользователей в единой базе данных доступа.

Протокол создан достаточно давно и поэтому поддерживается множеством сетевых утройств и сервисов.

### Функции компонента

Ключевые функции:

1. Прием запросов на аутентификацию по протоколу RADIUS;
2. Проверка первого фактора аутентификации &mdash; логина и пароля пользователя в Active Directory или Network Policy Server;
3. Проверка второго фактора аутентификации на дополнительном устройстве пользователя (обычно, телефон).

Дополнительные возможности:

* регистрация второго фактора непосредственно в VPN/VDI клиенте при первом подключении;
* настройка доступа на основе принадлежности пользователя к группе в Active Directory;
* избирательное включение второго фактора на основе принадлежности пользователя к группе в Active Directory;
* использование телефона пользователя из профиля Active Directory для отправки одноразового кода через СМС;
* настройка атрибутов ответа RADIUS на основе принадлежности пользователя к группе Active Directory;
* проксирование запросов и ответов Network Policy Server;

## Требования для установки компонента

* Компонент устанавливается на Linux сервер, протестирован на CentOS, Ubuntu, Debian, Astra Linux;
* Минимальные требования для сервера: 1 CPU, 2 GB RAM, 8 GB HDD (обеспечивают работу ОС и адаптера для 100 одновременных подключений &mdash; примерно 1500 пользователей);
* На сервере должен быть открыт порт 1812 (UDP) для приема запросов от Radius клиентов;
* Серверу с установленным компонентом необходим доступ к хосту api.multifactor.ru по TCP порту 443 (TLS) напрямую или через HTTP proxy;
* Для взаимодействия с Active Directory, компоненту нужен доступ к серверу домена по TCP порту 389 (схема LDAP) или 636 (схема LDAPS);
* Для взаимодействия с Network Policy Server, компоненту нужен доступ к NPS по UDP порту 1812.

## Установка

### Установка зависимостей

Компонент использует среду выполнения .NET 6 runtime, которая является бесплатной, открытой, разрабатывается компанией Microsoft и Open-Source сообществом. Среда выполнения не накладывает никаких ограничений на использование.

Для установки выполните команды:

#### CentOS 7

```shell
sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
sudo yum install aspnetcore-runtime-6.0
```

<a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-centos" target="_blank">https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-centos</a>

#### CentOS 8

> ⚠️ **Warning**  
> CentOS Linux 8 достигла раннего окончания жизни (EOL) 31 декабря 2021 года.  
> Дополнительные сведения см. на официальной <a href="https://www.centos.org/centos-linux-eol/" target="_blank">странице</a> EOL Для CentOS Linux.
> Из-за этого .NET не поддерживается в CentOS Linux 8.

Дополнительную информацию см. на <a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux-centos" target="_blank">странице</a>.  
См. также: <a href="https://learn.microsoft.com/ru-ru/dotnet/core/install/linux-rhel#supported-distributions">установка .NET на CentOS Stream</a>.

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

### Установка компонента

Создайте папку, скачайте и распакуйте актуальную версию компонента из <a href="https://github.com/MultifactorLab/multifactor-radius-adapter/releases/" target="_blank">GitHub</a>:

```shell
sudo mkdir /opt/multifactor /opt/multifactor/radius /opt/multifactor/radius/logs
sudo wget https://github.com/MultifactorLab/multifactor-radius-adapter/releases/latest/download/release_linux_x64.zip
sudo unzip release_linux_x64.zip -d /opt/multifactor/radius
```

Создайте системного пользователя mfa и дайте ему права на приложение:

```shell
sudo useradd -r mfa
sudo chown -R mfa: /opt/multifactor/radius/
sudo chmod -R 700 /opt/multifactor/radius/
```

Создайте службу

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

Включите автозапуск:

```shell
sudo systemctl enable multifactor-radius
```

## Конфигурация

Параметры работы компонента хранятся в файле ```/opt/multifactor/radius/multifactor-radius-adapter.dll.config``` в формате XML.

### Общие параметры

```xml
<!-- Адрес и порт (UDP) по которому адаптер будет принимать запросы на аутентификацию от клиентов -->
<!-- Если указать адрес 0.0.0.0, то адаптер будет слушать все сетевые интерфейсы-->
<add key="adapter-server-endpoint" value="0.0.0.0:1812"/>

<!-- Shared secret для аутентификации RADIUS клиентов -->
<add key="radius-shared-secret" value=""/>

<!--Где проверять первый фактор: ActiveDirectory или RADIUS или None (не проверять) -->
<add key="first-factor-authentication-source" value="ActiveDirectory"/>

<!--Адрес API Мультифактора -->
<add key="multifactor-api-url" value="https://api.multifactor.ru"/>
<!--Таймаут запросов в API Мультифактора, минимальное значение 65 секунд -->
<add key="multifactor-api-timeout" value="00:01:05"/>
<!-- Параметр NAS-Identifier для подключения к API Мультифактора - из личного кабинета -->
<add key="multifactor-nas-identifier" value=""/>
<!-- Параметр Shared Secret для подключения к API Мультифактора - из личного кабинета -->
<add key="multifactor-shared-secret" value=""/>

<!--Доступ к API Мультифактора через HTTP прокси (опционально)-->
<!--add key="multifactor-api-proxy" value="http://proxy:3128"/-->

<!-- Уровень логирования: 'Debug', 'Info', 'Warn', 'Error' -->
<add key="logging-level" value="Debug"/>
```

### Параметры подключения к Active Directory

Для проверки первого фактора в домене применимы следующие параметры:

```xml
<!--ActiveDirectory домен: в текущем примере domain.local на сервере 10.0.0.4 -->
<add key="active-directory-domain" value="ldaps://10.0.0.4/DC=domain,DC=local"/>

<!--Разрешать доступ только пользователям из указанной группы (не проверяется, если удалить настройку)-->
<add key="active-directory-group" value="VPN Users"/>
<!--Запрашивать второй фактор только у пользователей из указанной группы (второй фактор требуется всем, если удалить настройку)-->
<add key="active-directory-2fa-group" value="2FA Users"/>
<!--Использовать номер телефона из Active Directory для отправки одноразового кода в СМС (не используется, если удалить настройку)-->
<!--add key="use-active-directory-user-phone" value="true"/-->
<!--add key="use-active-directory-mobile-user-phone" value="true"/-->
```

При включении параметра ```use-active-directory-user-phone``` компонент будет использовать телефон, записанный на вкладке General. Формат телефона может быть любым.

<img src="https://multifactor.ru/img/radius-adapter/ra-ad-phone-source.png" width="300">

При включении параметра ```use-active-directory-mobile-user-phone``` компонент будет использовать телефон, записанный на вкладке Telephones в поле Mobile. Формат телефона также может быть любым.

<img src="https://multifactor.ru/img/radius-adapter/ra-ad-mobile-phone-source.png" width="300">

### Параметры подключения к внешнему RADIUS серверу

Для проверки первого фактора в RADIUS, например, в Network Policy Server применимы следующие параметры:

```xml
<!--Адрес (UDP) с которого адаптер будет подключаться к серверу -->
<add key="adapter-client-endpoint" value="192.168.0.1"/>
<!--Адрес и порт (UDP) сервера -->
<add key="nps-server-endpoint" value="192.168.0.10:1812"/>
```

### Дополнительные RADIUS атрибуты

Можно указать, какие атрибуты будет передавать компонент при успешной аутентификации, в том числе с проверкой вхождения пользователя в группу безопасности

```xml
<RadiusReply>
    <Attributes>
        <!--Это пример, можно использовать любые атрибуты-->
        <add name="Class" value="Super" />
        <add name="Fortinet-Group-Name" value="Users" when="UserGroup=VPN Users"/>
        <add name="Fortinet-Group-Name" value="Admins" when="UserGroup=VPN Admins"/>
    </Attributes>
</RadiusReply>
```

### Параметры проверки второго фактора

Следующие параметры помогут настроить обращение в API МУЛЬТИФАКТОР при проверке второго фактора:

```xml
<!-- Использовать указанный аттрибут в качестве идентификатора пользователя при проверке второго фактора-->
<add key="use-attribute-as-identity" value="mail"/>
<!-- Пропускать повторные аутентификации без запроса второго фактора в течение 1 часа 20 минут 10 секунд (кэширование отключено, если удалить настройку) -->
<add key="authentication-cache-lifetime" value="01:20:10" />
<!-- В случае недоступности API МУЛЬТИФАКТОР пропускать без проверки (по умолчанию), либо запрещать доступ (false) -->
<add key="bypass-second-factor-when-api-unreachable" value="true"/>
<!-- Автоматически присваивать членство в группах МУЛЬТИФАКТОР регистрирующимся пользователям -->
<add key="sign-up-groups" value="group1;Название группы 2"/>
```

### Журналирование

Существуют следующие параметры для настройки журналирования:

```xml
<!--Позволяет настроить шаблон логов, которые попадают в системный журнал -->
<add key="console-log-output-template" value="outputTemplate"/>
<!--Позволяет настроить шаблон логов, которые попадают в файл -->
<add key="file-log-output-template" value="outputTemplate"/>
```

В качестве ```outputTemplate``` выступает текстовый шаблон, который показывает системе ведения логов, как следует отформатировать сообщение. Например:

 ```sh
[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}
[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception} 
```

Подробнее про шаблоны можно прочитать [по ссылке.](https://github.com/serilog/serilog/wiki/Formatting-Output)

Также журналирование может вестись в формате json:

```xml
<add key="logging-format" value="format"/>
```

Для этого формата не применим текстовый шаблон, но можно выбрать один из следующих предустановленных форматтеров. Далее приведены возможные значения параметра ```format``` (регистр не важен):

* ```Json``` или ```JsonUtc```. Компактное представление логов, время в UTC.

   ```json
   {"@t":"2016-06-07T03:44:57.8532799Z","@m":"Hello, \"nblumhardt\"","@i":"7a8b9c0d","User":"nblumhardt"}
   ```

* ```JsonTz```. Компактное представление логов, отличается от ```JsonUtc``` форматом времени. В данном форматтере указано локальное время с часовым поясом.

   ```Json
   {"@t":"2023-11-23 17:16:29.919 +03:00","@m":"Hello, \"nblumhardt\"","@i":"7a8b9c0d","User":"nblumhardt"}
   ```

* ```Ecs```. Форматирует логи в соответствии с Elastic Common Schema.

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

## Запуск компонента

После настройки конфигурации запустите компонент:

```shell
sudo systemctl start multifactor-radius
```

Статус можно проверить командой:

```shell
sudo systemctl status multifactor-radius
```

## Журналы

Журналы работы компонента находятся в папке ```/opt/multifactor/radius/logs```, а также в системном журнале.

## Ограничения работы с Active Directory

* Linux версия адаптера **пока** не умеет работать с несколькими доменами, между которыми установлено доверие.
* Для работы с Active Directory используется простая проверка подлинности пароля пользователя. Настоятельно рекомендуем использовать схему LDAPS для шифрования трафика между адаптером и доменом (на сервере AD должен быть установлен сертификат, в т.ч. самоподписанный).

## Сценарии использования

С помощью компонента можно реализовать следующие сценарии:

* Двухфакторная аутентификация для VPN устройств [Cisco](https://multifactor.ru/docs/vpn/cisco-anyconnect-vpn-2fa/), [Fortigate](https://multifactor.ru/docs/vpn/fortigate-forticlient-vpn-2fa/), [CheckPoint](https://multifactor.ru/docs/vpn/checkpoint-remote-access-vpn-2fa/), Mikrotik, Huawei и других;
* Двухфакторная аутентификация [Windows VPN со службой Routing and Remote Access Service (RRAS)](https://multifactor.ru/docs/windows-2fa-rras-vpn/);
* Двухфакторная аутентификация [Microsoft Remote Desktop Gateway](https://multifactor.ru/docs/windows-2fa-remote-desktop-gateway/);
* Двухфакторная аутентификация [VMware Horizon](https://multifactor.ru/docs/vmware-horizon-2fa/);
* Двухфакторная аутентификация [Citrix Gateway](https://multifactor.ru/docs/citrix-radius-2fa/);
* Двухфакторная аутентификация Apache Guacamole;
* Двухфакторная аутентификация Wi-Fi точек доступа;

и многие другие...

## Лицензия

Обратите внимание на <a href="https://github.com/MultifactorLab/multifactor-radius-adapter/blob/master/LICENSE.ru.md" target="_blank">лицензию</a>. Она не дает вам право вносить изменения в исходный код Компонента и создавать производные продукты на его основе. Исходный код предоставляется в ознакомительных целях.
