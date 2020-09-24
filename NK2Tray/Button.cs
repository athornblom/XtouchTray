using NAudio.Midi;
using System;
using System.Diagnostics;
using System.Linq;

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
        Macro1,
        Macro2,
        Macro3,
        Macro4,
        Macro5,
        Macro6,
        Macro7,
        Macro8

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
