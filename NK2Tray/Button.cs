﻿using NAudio.Midi;
using System;
using System.Diagnostics;

namespace NK2Tray
{
    public enum ButtonType
    {
        MediaPlay,
        MediaStop,
        MediaPrevious,
        MediaNext,
        MediaRecord,
        McBtn,
        LayerA,
        LayerB,

    }

    public class Button
    {
        private bool activeHandling = false;

        private bool light = false;

        public ButtonType buttonType;
        public int controller;
        public MidiCommandCode commandCode;
        public int channel;
        public MidiOut midiOut;

        public Button(ref MidiOut midiOutRef, ButtonType butType, int cont, bool initialState, MidiCommandCode code=MidiCommandCode.ControlChange)
        {
            commandCode = code;
            channel = 1;
            buttonType = butType;
            controller = cont;
            midiOut = midiOutRef;
            SetLight(initialState);
        }

        public void SetLight(bool state)
        {
            light = state;
            if (commandCode == MidiCommandCode.ControlChange)
                midiOut.Send(new ControlChangeEvent(0, channel, (MidiController)(controller), state ? 127 : 0).GetAsShortMessage());
            else if (commandCode == MidiCommandCode.NoteOn)
                midiOut.Send(new NoteOnEvent(0, 1, controller, state ? 127 : 0, 0).GetAsShortMessage());
        }

        public bool HandleEvent(MidiInMessageEventArgs e, MidiDevice device)
        {
            if (!IsHandling())
            {
                SetHandling(true);

                if (e.MidiEvent.CommandCode != commandCode)
                    return false;

                int c;

                if (commandCode == MidiCommandCode.ControlChange)
                {
                    var me = (ControlChangeEvent)e.MidiEvent;

                    if (me.Channel != channel || me.ControllerValue != 127) // Only on correct channel and button-down (127)
                        return false;

                    c = (int)me.Controller;
                }
                else if (commandCode == MidiCommandCode.NoteOn)
                {
                    var me = (NoteEvent)e.MidiEvent;

                    if (me.Channel != channel || me.Velocity != 127) // Only on correct channel and button-down (127)
                        return false;

                    c = me.NoteNumber;
                }
                else
                    return false;

                if (c == controller)
                {
                    switch (buttonType)
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
                            device.LightShow();
                            break;
                        case ButtonType.McBtn:
                            if (light) // Light
                            {
                                SetLight(false);
                                light = false;
                                MidiDevice.McButtonState = false;
                            }
                            else
                            {
                                SetLight(true);
                                light = true;
                                MidiDevice.McButtonState = true;
                            }
                            break;
                        case ButtonType.LayerA:
                            device.buttonsMappingTable[85].
                            SetLight(true);
                            light = true;
                            MidiDevice.MainLayer = true;

                            break;
                        case ButtonType.LayerB:
                            device.buttonsMappingTable[84]
                            SetLight(true);
                            light = true;
                            MidiDevice.MainLayer = false;
                            break;
                      
                        default:
                            break;
                    }
                    return true;
                }
            }

            return false;
        }

        public bool IsHandling()
        {
            return activeHandling;
        }

        public void SetHandling(bool handling)
        {
            activeHandling = handling;
        }

        public bool GetLight() { return light; }

    }
}
