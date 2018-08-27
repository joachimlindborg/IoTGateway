# Waher.Service.PcSensor

The **Waher.Service.PcSensor** project defines an application that converts your PC into an IoT sensor, by publishing performace counters as 
sensor values.

The first time the application is run, it provides a simple console interface for the user to provide network credentials. 
These credentials are then stored in the **xmpp.config** file. Passwords are hashed.

When it is read for the first time, it also creates a file called [categories.xml](#categories-xml) which lists all performance counter categories found, and 
if they should be included in the data readout or not. If new categories are found during the runtime of the application, the file is updated. 
By default, new categories are not included. 

## Console interface

The console interface can be used for two purposes:

1. To enter credentials for the XMPP connection. This is done the first time the application is run.
2. To view XMPP communication. This is done if a sniffer is enabled in the first step.

![Sniff](../../Images/Waher.Service.PcSensor.1.png)

## Sensor data

The application publishes performance counter values as sensordata using XMPP, and the [IEEE XMPP IoT extensions](https://gitlab.com/IEEE-SA/XMPPI/IoT). 
Which performance counters to publish is defined in the [categories.xml](#categories-xml) file.

![Sniff](../../Images/Waher.Service.PcSensor.2.png)

## Chat interface

As the application is available through XMPP, it also publishes a chat interface:

![Sniff](../../Images/Waher.Service.PcSensor.3.png)

## Binary executable

You can test the application by downloading a [binary executable](../../Executables/Waher.Service.PcSensor.zip). If you don't have an XMPP client
you can use to chat with the sensor, or if the one you use does not support the XMPP IoT XEPs, you can also download the
[WPF client](../../Executables/Waher.Client.WPF.zip) available in the solution.

## Categories XML

After the first readout, a file called **categories.xml** is created. It includes all performance counter categories found. By default, no categories
are included in the readout. The file is updated if new categories are installed. To publish a category, set the **include** attribute to **true**.

In multi-instance categories, all instances are included by default, if not specified otherwise. To limit the category to certain instance names,
specify which ones using **Instance** elements.

### Example

Following is an example of a **categories.xml** file. Ellipsis has been used to shorten the example.

```XML
<?xml version="1.0" encoding="utf-8"?>
<Categories xmlns="http://waher.se/Schema/PerformanceCounterCategories.xsd">
	<Category name=".NET CLR-data" include="true" />
	<Category name=".NET CLR-n�tverk" include="true" />
	<Category name=".NET CLR-n�tverk 4.0.0.0" include="true" />
	<Category name=".NET-dataprovider f�r Oracle" include="false" />
	<Category name=".NET-dataprovider f�r SqlServer" include="false" />
	...
	<Category name="Process" include="false" />
	<Category name="Processor" include="true">
		<Instance name="_Total"/>
	</Category>
	<Category name="Processor f�r virtuell Hyper-V-v�xel" include="false" />
	...
	<Category name="XHCI Interrupter" include="false" />
	<Category name="XHCI TransferRing" include="false" />
</Categories>
```

## License

You should carefully read the following terms and conditions before using this software. Your use of this software indicates
your acceptance of this license agreement and warranty. If you do not agree with the terms of this license, or if the terms of this
license contradict with your local laws, you must remove any files from the **IoT Gateway** from your storage devices and cease to use it. 
The terms of this license are subjects of changes in future versions of the **IoT Gateway**.

You may not use, copy, emulate, clone, rent, lease, sell, modify, decompile, disassemble, otherwise reverse engineer, or transfer the
licensed program, or any subset of the licensed program, except as provided for in this agreement.  Any such unauthorised use shall
result in immediate and automatic termination of this license and may result in criminal and/or civil prosecution.

The [source code](https://github.com/PeterWaher/IoTGateway) and libraries provided in this repository is provided open for the following uses:

* For **Personal evaluation**. Personal evaluation means evaluating the code, its libraries and underlying technologies, including learning 
	about underlying technologies.

* For **Academic use**. If you want to use the following code for academic use, all you need to do is to inform the author of who you are, what 
	academic institution you work for (or study for), and in what projects you intend to use the code. All I ask in return is for an 
	acknowledgement and visible attribution to this repository, including a link, and that you do not redistribute the source code, or parts thereof 
	in the solutions you develop. Any solutions developed for academic use, that become commercial, require a commercial license.

* For **Security analysis**. If you perform any security analysis on the code, to see what security aspects the code might have, all that is 
	asked of you, is that you inform the author of any findings at least forty-five days before publication of the findings, so that any vulnerabilities 
	might be addressed. Such contributions are much appreciated and will be acknowledged.

Commercial use of the code, in part or in full, in compiled binary form, or its source code, requires
a **Commercial License**. Contact the author for details.

All rights to the source code are reserved and exclusively owned by [Waher Data AB](http://waher.se/). 
Any contributions made to the **IoT Gateway** repository become the intellectual property of [Waher Data AB](http://waher.se/).
If you're interested in using the source code, as a whole, or in part, you need a license agreement 
with the author. You can contact him through [LinkedIn](http://waher.se/).

This software is provided by the copyright holder and contributors "as is" and any express or implied warranties, including, but not limited to, 
the implied warranties of merchantability and fitness for a particular purpose are disclaimed. In no event shall the copyright owner or contributors 
be liable for any direct, indirect, incidental, special, exemplary, or consequential damages (including, but not limited to, procurement of substitute 
goods or services; loss of use, data, or profits; or business interruption) however caused and on any theory of liability, whether in contract, strict 
liability, or tort (including negligence or otherwise) arising in any way out of the use of this software, even if advised of the possibility of such 
damage.

The **IoT Gateway** is &copy; [Waher Data AB](http://waher.se/) 2016-2018. All rights reserved.
 
[![](../../Images/logo-WaherDataAB-300x58.png)](http://waher.se/)
