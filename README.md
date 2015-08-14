# SlackOverSMS
Interact with Slack over SMS

## Introduction
This really was meant to be a small short project. I wrote the initial working code in a couple hours. I cleaned up the code (less shitty, took out keys) the day after. Things will probably not work.

## Building
### Requirements
- Windows 10
- [ngrok](https://ngrok.com/)

A little backstory. I originally wrote this as a Windows 10 Universal App but I then found out that you can't receive packets from other processes in Universal apps (this is also true for Windows 8 apps). I had everything working except for the receiving SMS messages part so I copy pasted the code into a WPF app and fixed the references instead. You're going to need to fix the references to get this to build.

This needs to be running with Administrator privileges so launch Visual Studio 2015 as an Administrator.

Make sure the following references are in the project

- System.Runtime.WindowsRuntime.dll in C:\Windows\Microsoft.NET\Framework64\v4.0.30319
- Windows.Foundation.FoundationContract.winmd in C:\Program Files (x86)\Windows Kits\10\References\Windows.Foundation.FoundationContract\1.0.0.0
- Windows.Foundation.UniversalApiContract.winmd in C:\Program Files (x86)\Windows Kits\10\References\Windows.Foundation.UniversalApiContract\1.0.0.0
- Windows.Networking.Connectivity.WwanContract.winmd in C:\Program Files (x86)\Windows Kits\10\References\Windows.Networking.Connectivity.WwanContract\1.0.0.0
- Windows.WinMD in C:\Program Files (x86)\Windows Kits\10\UnionMetadata\Facade

I did this horrible thing with the help of [this post](http://blogs.msdn.com/b/eternalcoding/archive/2013/10/29/how-to-use-specific-winrt-api-from-desktop-apps-capturing-a-photo-using-your-webcam-into-a-wpf-app.aspx).

### Create a Secrets.cs
I have provided an example Secrets.cs file to show what should go in that file. Add your Slack and Twilio information. Also add the SMS users you want interacting with the service.

### Setup ngrok
Setup ngrok on a port. Add that port number to your Secrets.cs file. Link your forwarding port in your Twilio dashboard and set your request method to GET. [More info here](https://www.twilio.com/blog/2013/10/test-your-webhooks-locally-with-ngrok.html).

## Usage
If you somehow got the project building you are now ready to experience the wonders of Slack using SMS. Why would you want to use Slack over SMS if you can just use the Slack app? You have a Windows Phone and they still don't have an official app, that's why.

Launch the program. Copy the URL into a respectable browser, login, select your team, copy the code from your redirect url to the text box. Hit the button. The reason you have to do this is because the Web control in WPF is horribly outdated and doesn't work with Slack's site. The client should now fire up and you are ready to go!

The program reads in messages from the #general channel. General flow:

1. Slack user is @mentioned at the beginning of a message. If that @mention is a Slack user go to 2. If that @mention actually isn't a Slack user go to 3.
2. Try to find the Slack user's number in the list of SMS users in the Secrets.cs file. Go to 4
3. @mention. This mention will have a number associated with it in Secrets.cs Go to 4.
4. Send the Slack message to the user's number.

If that user sends messages to the Twilio number the program will read in the message and send out a message in the #general channel using Slackbot.

## Examples

Slack message:  
@golf1052: this is a stupid project

SMS sent to @golf1052's number:  
Sanders: this is a stupid project

-----

Slack message:  
hey @golf1052 whats up

No SMS is sent because the mention was not at the beginning of the message

## Issues
I wrote this README at 1 AM on a Friday.  
This program is hella janky and will probably crash on you.  
I "handle" a [server migration](https://api.slack.com/events/team_migration_started) but I haven't actually seen one so I don't know if the code to handle it works.  
I've already stopped using this project and will probably never fix any bugs because I won't find them.  
This is a pretty stupid project because you can just use the Slack app.
