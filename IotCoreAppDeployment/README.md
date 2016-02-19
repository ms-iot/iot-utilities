## IotCoreAppDeployment

The **IotCoreAppDeployment** utility has been created to simplify the task of creating and 
deploying Iot Core background applications.  It is an extensible program that currently
supports **Node.js**, **Python**, and **Arduino Wiring** apps.

### Usage

The intent for **IotCoreAppDeployment** is that a user can simply specify a source code file
and a target.  The tools should do everything else.  As such, there are a minimal set
of arguments that the tool accepts.
```
  IotCoreAppDeployment.exe -s (source) -n (target):

    -s (required)    Specify source input
    -n (required)    Speficy IoT Core device name or IP address

    -a               Specify the target architecture [ARM|X86] ... ARM is the default
    -d               If this is specified, the temp folder will not be deleted (this is useful for diagnosing problems).
    -f               Specify the configuration [Debug|Release] ... Debug is the default
    -g               Specify SignTool.exe full path ... if this is not provided, the registry is queried
    -k               Specify SDK version ... 10.0.10586.0 is the default
    -o               Specify full local path to output APPX to ... if this is not provided, files will not be saved
    -p               Specify target user password) ... p@ssw0rd is the default
    -t               Specify the temp working directory ... if nothing is specified a folder in the %temp% will be used
    -u               Specify target username ... Administrator is the default
    -w               Specify PowerShell.exe full path ... if this is not provided, the registry is queried
    -x               Specify MakeAppx.exe full path ... if this is not provided, the registry is queried
```
### Node.js

To deploy a **Node.js** application, create a .js file that minimally contains:
```
  var http = require('http');
  http.createServer(function (req, res) {
    res.writeHead(200, { 'Content-Type': 'text/plain' });
    res.end('Hello World\n');
  }).listen(<PORTNUMBER>)
```
Then call **IotCoreAppDeployment** to deploy to your ARM device (to deploy to an x86 device, 
specify `-a x86`):
```
  IotCoreAppDeployment.exe -s <.js file path> -n <target ip address or name> -a arm
```

### Python

To deploy a **Python** application, create a .py file that minimally contains:
```
  import http.server
  
  class RequestHandler(http.server.BaseHTTPRequestHandler):
    def do_HEAD(self):
        self.send_response(200)
        self.send_header("Content-type", "text/plain")
        self.end_headers()
    def do_GET(self):
        self.wfile.write(b"Hello World from Python!")

  httpd = http.server.HTTPServer(("", <PORTNUM>), RequestHandler)
  print('Started web server on port %d' % httpd.server_address[1])
  httpd.serve_forever()
```
Then call **IotCoreAppDeployment** to deploy to your X86 device (to deploy to an ARM device, 
specify `-a arm`):
```
    IotCoreAppDeployment.exe -s <.py file path> -n <target ip address or name> -a x86
```
### Arduino Wiring

To deploy an **Arduino Wiring** application, create a .ino file that minimally contains:
```
  void setup()
  {
    // put your setup code here, to run once:
  }
  void loop()
  {
    // put your main code here, to run repeatedly:
  }
```
Then call **IotCoreAppDeployment** to deploy to an ARM device (to deploy to an x86 device, 
specify `-a x86`):
```
    IotCoreAppDeployment.exe -s <.ino file path> -n <target ip address or name> -a arm
```

### Extensibility

The intent of the **IotCoreAppDeployment** utility is that it be extensible.  To add a new 
project type, a new DLL should be added to the installation directory that implements
`IotCoreAppProjectExtensibility.IProjectProvider` and `IotCoreAppProjectExtensibility.IProject`.

**IotCoreAppDeployment** follows this basic pattern:

1. Call `IProject.GetBaseProjectType` to get the underlying base Windows 10 Iot Core 
   Background Application type.  Currently, the only supported base project is the C++ 
   Background Application.
2. Call `IProject.GetAppxContents` to get any project specific files that need to be
   deployed.
3. Call `IProject.GetAppxContentChanges` to specify the changes required to specialize
   any files.
4. Call `IProject.BuildAsync` if any building or linking or generation is required.
5. Call `IProject.GetCapabilities` to specify the Capabilities that this project will require.
6. Call `IProject.GetAppxMapContents` to specify the required layout of the deployment.
7. Call `IProject.GetDependencies` to specify the required framework dependencies.  Currently,
   only the C++ framework dependencies are supported.

The Node.js and Python IProject implementations offer a fairly simple example of creating
a new project type.

The INO (Arduino Wiring) IProject implementation offers a more complex example that involves
building and linking.







