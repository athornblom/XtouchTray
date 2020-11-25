using NAudio.CoreAudioApi;
using NAudio.Midi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Diagnostics;

namespace NK2Tray
{
    public enum SendEvent
    {
        AssignedState,
        MuteState,
        ErrorState,
        MediaPlay,
        MediaStop,
        MediaPrevious,
        MediaNext,
        MediaRecord
    }

    public class MidiDevice
    {
        public MidiIn midiIn;
        public MidiOut midiOut;

        public List<Fader> faders;
        public List<Button> buttons;
        public Hashtable buttonsMappingTable;

        public AudioDevice audioDevices;
        public static bool McButtonState = false;
        public static bool MainLayer = true;

        public virtual string SearchString => "wobbo";

        public virtual FaderDef DefaultFaderDef => new FaderDef(false, 1f, 1, true, true, true, 0, 0, 0, 0, MidiCommandCode.ControlChange, MidiCommandCode.ControlChange, MidiCommandCode.ControlChange, MidiCommandCode.ControlChange);

        public MidiDevice()
        {
            Console.WriteLine($@"Initializing Midi Device {SearchString}");
        }

        public bool Found => (midiIn != null && midiOut != null);

        public void FindMidiIn()
        {
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                Console.WriteLine("MIDI IN: " + MidiIn.DeviceInfo(i).ProductName);
                if (MidiIn.DeviceInfo(i).ProductName.ToLower().Contains(SearchString))
                {
                    midiIn = new MidiIn(i);
                    Console.WriteLine($@"Assigning MidiIn: {MidiIn.DeviceInfo(i).ProductName}");
                    break;
                }
            }
        }

        public void FindMidiOut()
        {
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                Console.WriteLine("MIDI OUT: " + MidiOut.DeviceInfo(i).ProductName);
                if (MidiOut.DeviceInfo(i).ProductName.ToLower().Contains(SearchString))
                {
                    midiOut = new MidiOut(i);
                    Console.WriteLine($@"Assigning MidiOut: {MidiOut.DeviceInfo(i).ProductName}");
                    break;
                }
            }
        }

        public void ListenForMidi()
        {
            midiIn.MessageReceived += midiIn_MessageReceived;
            midiIn.ErrorReceived += midiIn_ErrorReceived;
            midiIn.Start();
        }

        public void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            Console.WriteLine(String.Format("Time {0} Message 0x{1:X8} Event {2}",
                e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        public virtual void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            // WindowTools.Dump(e.MidiEvent); // testing
            Debug.WriteLine(e.MidiEvent);


                foreach (var fader in faders)
                {
                    fader.HandleEvent(e);
                    fader.SetHandling(false);
                }
                
                //ControlChangeEvent midiController = null;
                //
                //try
                //{
                //    midiController = (ControlChangeEvent)e.MidiEvent;
                //}
                //catch (System.InvalidCastException exc)
                //{
                //    return;
                //}
                //
                //if (midiController == null)
                //    return;
                ////key UP...!
                //if (midiController.ControllerValue == 0)
                //    return;
                //
                //var obj = buttonsMappingTable[(int)midiController.Controller];
                //if (obj != null)
                //{
                //    Button button = (Button)obj;
                //    button.HandleEvent(e, this);
                //    button.SetHandling(false);
                //}
                //else
                {
                    foreach (var button in buttons)
                    {
                        HandleEvent(e, button);
                        button.SetHandling(false);
                    }
                }
            
        }
        public bool HandleEvent(MidiInMessageEventArgs e, Button btn)
        {
            if (!btn.IsHandling())
            {
                btn.SetHandling(true);

                if (e.MidiEvent.CommandCode != btn.commandCode)
                    return false;

                int c;

                if (btn.commandCode == MidiCommandCode.ControlChange)
                {
                    var me = (ControlChangeEvent)e.MidiEvent;

                    if (me.Channel != btn.channel || me.ControllerValue != 127) // Only on correct channel and button-down (127)
                        return false;

                    c = (int)me.Controller;
                }
                else if (btn.commandCode == MidiCommandCode.NoteOn)
                {
                    var me = (NoteEvent)e.MidiEvent;

                    if (me.Channel != btn.channel || me.Velocity != 127) // Only on correct channel and button-down (127)
                        return false;

                    c = me.NoteNumber;
                }
                else
                    return false;

                if (c == btn.controller)
                {
                    switch (btn.buttonType)
                    {
                        case ButtonType.MediaNext:
                            MediaTools.Next();
                            break;
                        case ButtonType.MediaPrevious:
                            MediaTools.Previous();
                            break;
                        case ButtonType.MediaStop:
                            MediaTools.Stop();
                            break;
                        case ButtonType.MediaPlay:
                            MediaTools.Play();
                            break;
                        case ButtonType.MediaRecord:
                            this.LightShow();
                            break;
                        case ButtonType.McBtn:
                            if (MainLayer) { 
                                if (btn.GetLight()) // Light
                                {
                                    btn.SetLight(false);
                                    McButtonState = false;
                                }
                                else
                                {
                                    btn.SetLight(true);
                                    McButtonState = true;
                                }
                            }
                            break;
                        case ButtonType.LayerA:
                            if(!MainLayer)
                            layerSwitch(btn, true);
                            break;
                        case ButtonType.LayerB:
                            if (MainLayer)
                                layerSwitch(btn,false);
                            break;
                        case ButtonType.Macro1:
                                MediaTools.button1(1,MainLayer);
                            break;
                        case ButtonType.Macro2:
                                MediaTools.button1(2, MainLayer);
                            break;
                        case ButtonType.Macro3:
                            MediaTools.button1(3, MainLayer);
                            break;
                        case ButtonType.Macro4:
                            MediaTools.button1(4, MainLayer);
                            break;
                        case ButtonType.Macro5:
                            MediaTools.button1(5, MainLayer);
                            break;
                        case ButtonType.Macro6:
                            MediaTools.button1(6, MainLayer);
                            break;
                        case ButtonType.Macro7:
                            MediaTools.button1(7, MainLayer);
                            break;
                        case ButtonType.Macro8:
                            MediaTools.button1(8, MainLayer);
                            break;

                        default:
                            break;
                    }
                    return true;
                }
            }

            return false;
        }
        public void layerSwitch(Button btn,bool layerA)
        {

            TurnOfAllLights();
            btn.SetLight(true);
            MainLayer = layerA;
            if (MainLayer)
            {
                foreach (var btn2 in buttons)
                {
                    if(!btn2.buttonType.Equals("LayerB"))
                    SetLight(btn2.controller, btn2.GetLight());
                }
                SetLight(btn.controller +1, false);
            }
        }

        public void TurnOfAllLights()
        {
            foreach (var btn in buttons)
            {
                SetLight(btn.controller, false);
            }
            //foreach (var i in Enumerable.Range(0, 128))
            //   midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)i, 0).GetAsShortMessage());
        }
        public virtual void ResetAllLights() { }

        public virtual void LightShow() { }

        public virtual void SetVolumeIndicator(int fader, float level) { }

        public virtual void SetLight(int controller, bool state) {}

        public virtual void InitFaders()
        {
            faders = new List<Fader>();
        }

        public virtual void InitButtons()
        {
            buttons = new List<Button>();
        }

        public void SetCurve(float pow)
        {
            faders.ForEach(fader => fader.SetCurve(pow));
        }

        public void LoadAssignments()
        {
            bool foundAssignments = false;

            foreach (var fader in faders)
            {
                Console.WriteLine("Getting setting: " + fader.faderNumber.ToString());
                var ident = ConfigSaver.GetAppSettings(fader.faderNumber.ToString());

                Console.WriteLine("Got setting: " + ident);
                if (ident != null)
                {
                    if (ident.Equals("__FOCUS__"))
                    {
                        foundAssignments = true;
                        fader.Assign(new MixerSession("", audioDevices, "Focus", SessionType.Focus));
                    }
                    else if (ident.Equals("__MASTER__") || (ident.Substring(0, Math.Min(10, ident.Length)).Equals("__MASTER__")))
                    {
                        foundAssignments = true;
                        MMDevice mmDevice = audioDevices.GetDeviceByIdentifier(ident.IndexOf("|") >= 0 ? ident.Substring(ident.IndexOf("|")+1) : "");
                        fader.Assign(new MixerSession(mmDevice.ID, audioDevices, "Master", SessionType.Master));
                    }                    
                    else if (ident.Length > 0)
                    {
                        foundAssignments = true;
                        var matchingSession = audioDevices.FindMixerSession(ident);
                        if (matchingSession != null)
                            fader.Assign(matchingSession);
                        else
                            fader.AssignInactive(ident);
                    }
                    else
                    {
                        fader.Unassign();
                    }
                }
            }            
            
            // Load fader 8 as master volume control as default if no faders are set
            if (!foundAssignments)
            {
                if (faders.Count > 0)
                {
                    faders.Last().Assign(new MixerSession(audioDevices.GetDeviceByIdentifier("").ID, audioDevices, "Master", SessionType.Master));
                    SaveAssignments();
                }
            }
            
        }

        public void SaveAssignments()
        {
            Console.WriteLine("Saving Assignments");
            foreach (var fader in faders)
            {
                if (fader.assigned)
                {
                    if (fader.assignment.sessionType == SessionType.Master)
                        ConfigSaver.AddOrUpdateAppSettings(fader.faderNumber.ToString(), "__MASTER__|" + fader.assignment.parentDeviceIdentifier );
                    else if (fader.assignment.sessionType == SessionType.Focus)
                        ConfigSaver.AddOrUpdateAppSettings(fader.faderNumber.ToString(), "__FOCUS__");
                    else
                        ConfigSaver.AddOrUpdateAppSettings(fader.faderNumber.ToString(), fader.assignment.sessionIdentifier);
                }
                else
                    ConfigSaver.AddOrUpdateAppSettings(fader.faderNumber.ToString(), "");
            }
        }
    

    }
}
