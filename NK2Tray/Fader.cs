﻿using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NK2Tray
{
    public class FaderDef
    {
        public bool delta;
        public float range;
        public int channel;
        public bool selectPresent;
        public bool mutePresent;
        public bool recordPresent;
        public int faderOffset;
        public int selectOffset;
        public int muteOffset;
        public int recordOffset;
        public MidiCommandCode faderCode;
        public MidiCommandCode selectCode;
        public MidiCommandCode muteCode;
        public MidiCommandCode recordCode;
        public int faderChannelOverride;
        public int selectChannelOverride;
        public int muteChannelOverride;
        public int recordChannelOverride;

        public FaderDef(bool _delta, float _range, int _channel,
            bool _selectPresent, bool _mutePresent, bool _recordPresent,
            int _faderOffset, int _selectOffset, int _muteOffset, int _recordOffset,
            MidiCommandCode _faderCode, MidiCommandCode _selectCode, MidiCommandCode _muteCode, MidiCommandCode _recordCode,
            int _faderChannelOverride = -1, int _selectChannelOverride = -1, int _muteChannelOverride = -1, int _recordChannelOverride = -1)
        {
            delta = _delta;
            range = _range;
            channel = _channel;
            selectPresent = _selectPresent;
            mutePresent = _mutePresent;
            recordPresent = _recordPresent;
            faderOffset = _faderOffset;
            selectOffset = _selectOffset;
            muteOffset = _muteOffset;
            recordOffset = _recordOffset;
            faderCode = _faderCode;
            selectCode = _selectCode;
            muteCode = _muteCode;
            recordCode = _recordCode;
            faderChannelOverride = _faderChannelOverride;
            selectChannelOverride = _selectChannelOverride;
            muteChannelOverride = _muteChannelOverride;
            recordChannelOverride = _recordChannelOverride;
        }
    }

    public class Fader
    {
        private bool activeHandling = false;

        private bool selectLight = false;
        private bool muteLight = false;
        private bool recordLight = false;

        public int faderNumber;
        public FaderDef faderDef;
        public MixerSession assignment;
        public bool assigned;
        public MidiOut midiOut;
        public MidiDevice parent;
        public string identifier;
        public string applicationPath;
        public string applicationName;
        

        public float[] steps;
        public float pow;

        public Fader(MidiDevice midiDevice, int faderNum)
        {
            parent = midiDevice;
            midiOut = midiDevice.midiOut;
            faderNumber = faderNum;
            faderDef = parent.DefaultFaderDef;
            SetCurve(1f);
        }

        public Fader(MidiDevice midiDevice, int faderNum, FaderDef _faderDef)
        {
            parent = midiDevice;
            midiOut = midiDevice.midiOut;
            faderNumber = faderNum;
            faderDef = _faderDef;
            SetCurve(1f);
        }

        public void SetCurve(float _pow)
        {
            pow = _pow;
            steps = calculateSteps();
            parent.SetVolumeIndicator(faderNumber, assignment != null ? assignment.GetVolume() : -1);
        }

        private float[] calculateSteps()
        {
            if (!faderDef.delta) return new float[0];

            return Enumerable.Range(0, (int)faderDef.range + 1).Select(stage => getVolFromStage(stage)).ToArray();
        }

        public float getVolFromStage(int stage)
        {
            return (float)Math.Pow((double)stage / faderDef.range, pow);
        }

        private int inputController => faderNumber + faderDef.faderOffset;
        private int selectController => faderNumber + faderDef.selectOffset;
        private int muteController => faderNumber + faderDef.muteOffset;
        private int recordController => faderNumber + faderDef.recordOffset;

        public void ResetLights()
        {
            SetSelectLight(false);
            SetMuteLight(false);
            SetRecordLight(false);
        }

        public void Assign(MixerSession mixerSession)
        {
            assigned = true;
            assignment = mixerSession;
            identifier = mixerSession.sessionIdentifier;
            convertToApplicationPath(identifier);
            applicationName = mixerSession.label;
            SetSelectLight(true);
            SetRecordLight(false);
            SetMuteLight(mixerSession.GetMute());

            if (faderDef.delta)
                parent.SetVolumeIndicator(faderNumber, mixerSession.GetVolume());
        }

        public void AssignInactive(string ident)
        {
            identifier = ident;
            convertToApplicationPath(identifier);
            assigned = false;
            SetSelectLight(true);
            SetRecordLight(true);
            SetMuteLight(false);
        }

        public void Unassign()
        {
            assigned = false;
            assignment = null;
            SetSelectLight(false);
            SetRecordLight(false);
            SetMuteLight(false);
            identifier = "";
            if (faderDef.delta)
                parent.SetVolumeIndicator(faderNumber, -1);
        }

        private void convertToApplicationPath(string ident)
        {
            if (
                ident != null
                && ident != ""
                && !ident.StartsWith("#")
                && !ident.Substring(0, Math.Min(10, ident.Length)).Equals("__MASTER__")
                && ident != "__FOCUS__"
            ) // TODO cleaner handling of special fader types
            {
                //"{0.0.0.00000000}.{...}|\\Device\\HarddiskVolume8\\Users\\Dr. Locke\\AppData\\Roaming\\Spotify\\Spotify.exe%b{00000000-0000-0000-0000-000000000000}"
                int deviceIndex = ident.IndexOf("\\Device");
                int endIndex = ident.IndexOf("%b{");
                ident = ident.Substring(deviceIndex, endIndex - deviceIndex);
                applicationPath = DevicePathMapper.FromDevicePath(ident);
            }
        }

        public void SetSelectLight(bool state)
        {
            selectLight = state;
            parent.SetLight(selectController, state);
        }

        public void SetMuteLight(bool state)
        {
            muteLight = state;
            parent.SetLight(muteController, state);
        }

        public void SetRecordLight(bool state)
        {
            recordLight = state;
            parent.SetLight(recordController, state);
        }

        public bool GetSelectLight() { return selectLight; }

        public bool GetMuteLight() { return muteLight; }

        public bool GetRecordLight() { return recordLight; }

        public bool Match(int faderNumber, MidiEvent midiEvent, MidiCommandCode code, int offset, int channel = -1)
        {
            if (channel < 0)
                channel = faderDef.channel;
            if (midiEvent.Channel != channel)
                return false;
            if (midiEvent.CommandCode != code)
                return false;
            if (code == MidiCommandCode.ControlChange)
            {
                var me = (ControlChangeEvent)midiEvent;
                if ((int)me.Controller == faderNumber + offset)
                    return true;
            }
            else if (code == MidiCommandCode.NoteOn)
            {
                var me = (NoteEvent)midiEvent;
                if (me.NoteNumber == faderNumber + offset)
                    return true;
            }
            else if (code == MidiCommandCode.PitchWheelChange)
            {
                return true;
            }

            return false;
        }

        public int GetValue(MidiEvent midiEvent)
        {
            if (midiEvent.CommandCode == MidiCommandCode.ControlChange)
            {
                var me = (ControlChangeEvent)midiEvent;
                return me.ControllerValue;
            }
            else if (midiEvent.CommandCode == MidiCommandCode.NoteOn)
            {
                var me = (NoteEvent)midiEvent;
                return me.Velocity;
            }
            else if (midiEvent.CommandCode == MidiCommandCode.PitchWheelChange)
            {
                var me = (PitchWheelChangeEvent)midiEvent;
                return me.Pitch;
            }

            return 0;
        }

        public List<Fader> GetMatchingFaders()
        {
            MixerSession focusMixerSession = null;

            bool haveFocusSlider = parent.faders.Any(fader => (
                fader.assignment != null
                && fader.assignment.sessionType == SessionType.Focus
            ));

            if (haveFocusSlider)
            {
                int pid = WindowTools.GetForegroundPID();
                focusMixerSession = assignment.devices.FindMixerSession(pid);
            }

            if (assignment == null)
                return parent.faders.FindAll(fader => fader.assignment == null);

            if (assignment.sessionType == SessionType.Master)
            {
                return parent.faders.FindAll(fader =>
                {
                    if (fader == this) return true;
                    if (fader.assignment == null) return false;
                    if (fader.assignment.sessionType != SessionType.Master) return false;

                    return fader.assignment.parentDeviceIdentifier == assignment.parentDeviceIdentifier;
                });
            }

            if (assignment.sessionType == SessionType.Focus)
            {
                return parent.faders.FindAll(fader =>
                {
                    if (fader == this) return true;
                    if (fader.assignment == null) return false;
                    if (fader.assignment.sessionType == SessionType.Focus) return true;

                    return fader.assignment.HasCrossoverProcesses(focusMixerSession);
                });
            }

            if (assignment.sessionType == SessionType.Application)
            {
                return parent.faders.FindAll(fader =>
                {
                    if (fader == this) return true;
                    if (fader.assignment == null) return false;

                    if (fader.assignment.sessionType == SessionType.Application)
                        return fader.assignment.HasCrossoverProcesses(assignment);

                    if (fader.assignment.sessionType == SessionType.Focus)
                        return assignment.HasCrossoverProcesses(focusMixerSession);

                    return false;
                });
            }

            if (assignment.sessionType == SessionType.SystemSounds)
            {
                return parent.faders.FindAll(fader => (
                    fader.assignment != null
                    && fader.assignment.sessionType == SessionType.SystemSounds
                ));
            }

            return new List<Fader>();
        }

        public bool HandleEvent(MidiInMessageEventArgs e)
        {
            if (!IsHandling())
            {
                SetHandling(true);

                //if loaded inactive, search again
                if (!assigned && identifier != null && !identifier.Equals(""))
                {
                    assignment = parent.audioDevices.FindMixerSession(identifier);
                    if (assignment != null)
                    {
                        assigned = true;
                    }
                }

                // Fader match
                if (assigned && Match(faderNumber, e.MidiEvent, faderDef.faderCode, faderDef.faderOffset, faderDef.faderChannelOverride))
                {
                    if (MidiDevice.MainLayer)
                    {
                        if (assignment.sessionType == SessionType.Application)
                        {
                            MixerSession newAssignment = assignment.devices.FindMixerSession(assignment.sessionIdentifier); //update list for re-routered app, but only overrides
                            if (newAssignment == null)                                                                      //if there is new assignments, otherwise, there is no more a inactive
                            {                                                                                               //MixerSession to hold the label
                                SetRecordLight(true);
                                return true;
                            }
                            assignment = newAssignment;
                        }

                        float newVol;

                        if (faderDef.delta)
                        {
                            var val = GetValue(e.MidiEvent);
                            var volNow = assignment.GetVolume();
                            var nearestStep = steps.Select((x, i) => new { Index = i, Distance = Math.Abs(volNow - x) }).OrderBy(x => x.Distance).First().Index;
                            int nextStepIndex;

                            var volumeGoingDown = val > faderDef.range / 2;

                            if (volumeGoingDown)
                                nextStepIndex = Math.Max(nearestStep - 1, 0);
                            else
                                nextStepIndex = Math.Min(nearestStep + 1, steps.Length - 1);

                            newVol = steps[nextStepIndex];
                            assignment.SetVolume(newVol);
                        }
                        else
                        {
                            newVol = getVolFromStage(GetValue(e.MidiEvent));
                            assignment.SetVolume(newVol);
                        }

                        if (assignment.IsDead())
                        {
                            SetRecordLight(true);

                            return true;
                        }

                        List<Fader> fadersToAffect = GetMatchingFaders();
                        fadersToAffect.ForEach(fader => fader.parent.SetVolumeIndicator(fader.faderNumber, newVol));
                    }
                    else
                    {
                        //layer B implementation
                    }
                    return true;
                }
                //<--------------------------------------------------- >
                //                        buttons
                //<--------------------------------------------------- >

              
                // Select match
                if (
                    faderDef.selectPresent
                    && Match(faderNumber, e.MidiEvent, faderDef.selectCode, faderDef.selectOffset, faderDef.selectChannelOverride)
                    && MidiDevice.McButtonState == true
                )
                {
                    if (GetValue(e.MidiEvent) != 127) // Only on button-down
                        return true;

                    if (MidiDevice.MainLayer)
                    {
                        Console.WriteLine($@"Attempting to assign current window to fader {faderNumber}");
                        if (assigned)
                        {
                            Unassign();
                            parent.SaveAssignments();
                        }
                        else
                        {
                            var pid = WindowTools.GetForegroundPID();
                            var mixerSession = parent.audioDevices.FindMixerSession(pid);
                            if (mixerSession != null)
                            {
                                Assign(mixerSession);
                                parent.SaveAssignments();
                            }
                            else
                                Console.WriteLine($@"MixerSession not found for pid {pid}");
                        }
                    }
                    else
                    {
                        //Layer B implementation
                    }
                    return true;
                }

                // Mute match
                if (
                    faderDef.mutePresent
                    && assigned
                    && Match(faderNumber, e.MidiEvent, faderDef.muteCode, faderDef.muteOffset, faderDef.muteChannelOverride)
                    && MidiDevice.McButtonState == false
                )
                {
                   if (GetValue(e.MidiEvent) != 127) // Only on button-down
                       return true;

                    if (MidiDevice.MainLayer) { 
                        var muteStatus = assignment.ToggleMute();
                        SetMuteLight(muteStatus);

                        if (assignment.IsDead()) // hmmm tror inte detta funkar
                        {
                            SetRecordLight(true);

                            return true;
                        }

                        List<Fader> fadersToAffect = GetMatchingFaders();
                        fadersToAffect.ForEach(fader => fader.SetMuteLight(muteStatus));
                    }
                    else
                    {
                        //Layer B implementation
                    }

                    return true;
                }

                // Record match // hmm va e ens detta??
                if (
                    faderDef.recordPresent
                    && assigned
                    && applicationPath != null
                    && Match(faderNumber, e.MidiEvent, faderDef.recordCode, faderDef.recordOffset, faderDef.recordChannelOverride)
                )
                {
                    if (GetValue(e.MidiEvent) != 127) // Only on button-down
                        return true;
                    if (MidiDevice.MainLayer)
                    {
                        if (WindowTools.IsProcessByNameRunning(applicationName))
                            SetRecordLight(false);
                        else
                        {
                            WindowTools.StartApplication(applicationPath);
                        }
                    }
                    else
                    {
                        //LayerB implementation 
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
    }
}
