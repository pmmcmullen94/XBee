XBee
====

.NET library for [XBee wireless controllers](http://www.digi.com/xbee/) available as a [nuget package](https://www.nuget.org/packages/XBee/).

 * Support for Series1, Series 2, and 900HP
 * Simple async/await command and query model
 * [.NET Rx](https://rx.codeplex.com/)  support for async receive and sampling.


###Features###

 * Local and remote device discovery
 * Local and remote device configuration
 * Pin configuration
 * Pin control
 * Pin monitoring
 * Digital and analog sample monitoring via events or [.NET Rx](https://rx.codeplex.com/)
 * Data transmit
 * Data receive via events or [.NET Rx](https://rx.codeplex.com/)

###Quick Start###

Here is a simple example with a coordinator on COM3 and an arbitrary number of end devices that we're going to configure and monitor for sampling.

<strong>Ensure the coordinator is in API Mode 1</strong>

```C#
var controller = new XBeeController();

// setup a simple callback for each time we discover a node
controller.NodeDiscovered += async (sender, args) => 
{
    Console.WriteLine("Discovered {0}", args.Name);
    
	// setup some pins
    await args.Node.SetInputOutputConfiguration(InputOutputChannel.Channel2, InputOutputConfiguration.DigitalIn);
    await args.Node.SetInputOutputConfiguration(InputOutputChannel.Channel3, InputOutputConfiguration.AnalogIn);
    
	// set sample rate
    await args.Node.SetSampleRate(TimeSpan.FromSeconds(5));
    
	// register callback for sample recieved from this node
	// TODO: in practice you would want to make sure you only subscribe once (or better yet use Rx)
    args.Node.SampleReceived += (node, sample) => Console.WriteLine("Sample recieved: {0}", sample);
}

// open the connection to our coordinator
await controller.OpenAsync("COM3", 9600);

// now discover the network, which will trigger the NodeDiscovered callback for each node found
await controller.DiscoverNetwork();

Console.ReadKey();

// wait for the samples to flow in...

```

If you don't know a priori what port the XBee will be attached to you can also scan for it:

```c#
var controller = await XBeeController.FindAndOpen(SerialPort.GetPortNames(), 9600);

if(controller != null)
   // ...
```

###Nodes###

The XBeeController class represents the local serial attached XBee API.  This would typically be a coordinator but could be any device to be controlled via a serial port.

While the controller represents the API, if we want to control the node itself we need to access the local node property.

```c#
var localNode = controller.Local;
// which is the same as calling await controller.GetNodeAsync(null);

var serialNumber = await localNode.GetSerialNumber();
// etc
```

This allows us to treat the local node and remote nodes in the same fashion.

```c#
var remoteNode = await controller.GetNodeAsync(address);
var serialNumber = await remoteNode.GetSerialNumber();
```

The address for the remote node can be determined in a number of ways.  Either connect the remote node to a serial port and use one of the X-CTU utilities (or the above code) or use network discovery.

Now that we have some nodes, let's do something with them...

###Command and Events###

XBees are based on a sort of command-event model where the coordinator is either telling the XBee to do something or the XBee is telling the coordinator that something happened.

####Commands####

The first type of command is what XBee calls AT commands.  An example is a command that can be used to configure pins on the XBee, setting pins high or low or reserving them for input.

```c#
await node.SetInputOutputConfiguration(InputOutputChannel.Channel4, InputOutputConfiguration.DigitalHigh);
```

This will force pin DIO4 high.  Note that which physical pin this translates to depends on the model.

Below is a table summarizing the commands supported by this library.

####AT Commands####

|       | Description            | Query                            | Command                          | S1 | S2 | Pro900 |
|:-----:|:-----------------------|:---------------------------------|:---------------------------------|:--:|:--:|:------:|
| AI    | Association Indicator  | GetAssociationAsync              | SetAssociationAsync              |    | x  |        |
| AP    | API Mode               | GetApiModeAsync                  | SetApiModeAsync                  | x  | x  |    x   |
| BD    | Interface Data Rate    | GetBaudRateAsync                 | SetBaudRateAsync                 | x  | x  |    x   |
| CE    | Coordinator Enable     | IsCoordinatorAsync               | SetCoordinatorAsync              | x  |    |        |
| CH    | Channel                | IsCoordinatorAsync               | SetCoordinatorAsync              | x  |    |        |
| CN    | Exit Command Mode      | --                               | ExitCommandModeAsync             | x  | x  |    x   |
| D(N)  | DIO Configuration      | GetInputOutputConfigurationAsync | SetInputOutputConfigurationAsync | x  | x  |    x   |
| DH/DL | Destination Address    | GetDestinationAddressAsync       | SetDestinationAddressAsync       | x  | x  |    x   |
| EE    | Encryption Enable      | IsEncryptionEnabledAsync         | SetEncryptionEnabledAsync        | x  | x  |    x   |
| HV    | Hardware Version       | HardwareVersion (property)       | --                               | x  | x  |    x   |
| IC    | Input Change           | GetChangeDetectionChannelsAsync  | SetChangeDetectionChannelsAsync  | x  | x  |    x   |
| IR    | Sample Rate            | GetSampleRateAsync               | SetSampleRateAsync               | x  | x  |    x   |
| IS    | Force Sample           | --                               | ForceSampleAsync                 | x  | x  |    x   |
| KY    | Encryption Key         | --                               | SetEncryptionKeyAsync            | x  | x  |    x   |
| MY    | Source Address         | GetNetworkAddressAsync           | SetNetworkAddressAsync           | x  | x  |    x   |
| ND    | Node Discovery         | --                               | DiscoverNetworkAsync             | x  | x  |    x   |
| NI    | Node Identifier        | GetNodeIdentifierAsync           | SetNodeIdentifierAsync           | x  | x  |    x   |
| SH/SL | Serial Number          | GetSerialNumberAsync             | --                               | x  | x  |    x   |
| SM    | Sleep Mode             | GetSleepModeAsync                | SetSleepModeAsync                | x  | x  |    x   |
| SO    | Sleep Mode Options     | GetSleepOptionsAsync             | SetSleepOptionsAsync             | x  |    |    x   |
| WR    | Write Command          | --                               | WriteChangesAsync                | x  | x  |    x   |

All other commands are currently unsupported but feel free to file an issue if you'd like to see something that isn't here.

####Serial Data####

The second type of command involves sending arbitrary serial data to a node.  In the simplest case this can act as a transparent passthrough as most XBees will pass the serial data to their local UART.  However, in the case of programmable XBees it is possible to intercept the serial data and store it, interpret it, etc.

```c#
await node.TransmitDataAsync(Encoding.UTF8.GetBytes("Hello!"));
```

###Events###

Somewhat confusingly, XBees have two different mechanisms for asychronously sending data to the coordinator.  The first is sampling and the second is serial data.  Samples coorespond to our pin example from above and represent the "input" part of GPIO.

As such, we can configure a pin to take and return a sample to the coordinator.  

```c#
// subscribe to the node
node.SampleReceived += (o, eventArgs) => Console.WriteLine(eventArgs.DigitalSampleState);

// configure a pin for digital sampling
await node.SetInputOutputConfiguration(InputOutputChannel.Channel5, InputOutputConfiguration.DigitalIn);
```

At this point the node is set to send samples from pin DIO5 but not necessarily to take samples.  There are three ways to trigger a sample: forced, periodic, or change detect.

```c#
await node.ForceSample(); // force
```
```c#
await node.SetSampleRate(TimeSpan.FromSeconds(5)); // periodic
```
```c#
await node.SetChangeDetectionChannels(DigitalSampleChannels.Input5); // change detect
```

The second mechanism for asynchronous remote data transmit is simply the receive side of the transparent serial channel.

```c#
node.DataReceived += (o, eventArgs) => Console.WriteLine("Received {0} bytes", eventArgs.Data.Length);
```

Again, this data would either be supplied by the external UART on the remote node or by the microcontroller on the remote node.
