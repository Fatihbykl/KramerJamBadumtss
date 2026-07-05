using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;

namespace ClockworkGearslinger.Core
{
    /// <summary>
    /// A lightweight parser to extract beat timestamps from a standard MIDI (.mid) file.
    /// Reads Note-On events to perfectly sync gameplay with the audio track.
    /// </summary>
    public static class MidiBeatParser
    {
        public static List<float> Parse(byte[] midiBytes, int targetMidiNote = -1, float fallbackBpm = 120f, bool forceFallbackBpm = false)
        {
            int offset = 0;
            
            // Read MThd
            if (ReadString(midiBytes, ref offset, 4) != "MThd") return null;
            int headerLen = ReadInt32(midiBytes, ref offset);
            int format = ReadInt16(midiBytes, ref offset);
            int trackCount = ReadInt16(midiBytes, ref offset);
            int division = ReadInt16(midiBytes, ref offset); // Ticks per quarter note
            
            // Note: If division top bit is 1, it's SMPTE time. We'll assume PPQN (top bit 0) for now.
            if ((division & 0x8000) != 0) 
            {
                Debug.LogError("SMPTE time format not supported. Please export MIDI using PPQN.");
                return null;
            }

            // Extract all events with their absolute ticks
            List<MidiEvent> allEvents = new List<MidiEvent>();
            HashSet<int> foundNotes = new HashSet<int>();

            for (int i = 0; i < trackCount; i++)
            {
                if (offset >= midiBytes.Length) break;
                string chunkType = ReadString(midiBytes, ref offset, 4);
                int chunkLen = ReadInt32(midiBytes, ref offset);
                int trackEnd = offset + chunkLen;

                if (chunkType != "MTrk")
                {
                    offset = trackEnd;
                    continue;
                }

                int absoluteTicks = 0;
                byte runningStatus = 0;

                while (offset < trackEnd)
                {
                    int deltaTicks = ReadVLQ(midiBytes, ref offset);
                    absoluteTicks += deltaTicks;

                    byte status = midiBytes[offset];
                    if (status < 0x80)
                    {
                        status = runningStatus;
                    }
                    else
                    {
                        offset++;
                        runningStatus = status;
                    }

                    if (status == 0xFF) // Meta event
                    {
                        byte metaType = midiBytes[offset++];
                        int metaLen = ReadVLQ(midiBytes, ref offset);
                        
                        if (metaType == 0x51 && metaLen == 3) // Set Tempo
                        {
                            int mpqn = (midiBytes[offset] << 16) | (midiBytes[offset+1] << 8) | midiBytes[offset+2];
                            if (!forceFallbackBpm)
                            {
                                allEvents.Add(new MidiEvent { Type = EventType.Tempo, AbsoluteTicks = absoluteTicks, Value = mpqn });
                            }
                        }
                        offset += metaLen;
                    }
                    else if (status == 0xF0 || status == 0xF7) // Sysex
                    {
                        int sysexLen = ReadVLQ(midiBytes, ref offset);
                        offset += sysexLen;
                    }
                    else // Standard MIDI event
                    {
                        byte cmd = (byte)(status & 0xF0);
                        if (cmd == 0xC0 || cmd == 0xD0)
                        {
                            offset += 1;
                        }
                        else
                        {
                            byte data1 = midiBytes[offset++];
                            byte data2 = midiBytes[offset++];

                            if (cmd == 0x90 && data2 > 0) // Note On with velocity > 0
                            {
                                foundNotes.Add(data1);
                                if (targetMidiNote == -1 || targetMidiNote == data1)
                                {
                                    allEvents.Add(new MidiEvent { Type = EventType.NoteOn, AbsoluteTicks = absoluteTicks, Value = data1 });
                                }
                            }
                        }
                    }
                }
            }

            // Sort events by absolute ticks
            allEvents.Sort((a, b) => a.AbsoluteTicks.CompareTo(b.AbsoluteTicks));

            // Convert absolute ticks to absolute time in seconds
            List<float> noteOnTimes = new List<float>();
            
            int currentMpqn = Mathf.RoundToInt((60f / fallbackBpm) * 1000000f);
            double currentAbsoluteTime = 0.0;
            int lastTicks = 0;

            foreach (var ev in allEvents)
            {
                int ticksDiff = ev.AbsoluteTicks - lastTicks;
                double secondsDiff = (ticksDiff / (double)division) * (currentMpqn / 1000000.0);
                currentAbsoluteTime += secondsDiff;
                lastTicks = ev.AbsoluteTicks;

                if (ev.Type == EventType.Tempo)
                {
                    currentMpqn = ev.Value;
                }
                else if (ev.Type == EventType.NoteOn)
                {
                    noteOnTimes.Add((float)currentAbsoluteTime);
                }
            }

            if (foundNotes.Count > 0)
            {
                string notesStr = string.Join(", ", foundNotes.OrderBy(n => n));
                Debug.Log($"[MidiBeatParser] Found the following MIDI notes in the file: {notesStr}");
            }

            // Remove duplicates which might happen if multiple tracks have the same note at the same time
            return noteOnTimes.Distinct().OrderBy(t => t).ToList();
        }

        private enum EventType { NoteOn, Tempo }
        
        private class MidiEvent 
        {
            public EventType Type;
            public int AbsoluteTicks;
            public int Value;
        }

        private static int ReadVLQ(byte[] data, ref int offset)
        {
            int value = 0;
            byte b;
            do
            {
                b = data[offset++];
                value = (value << 7) | (b & 0x7F);
            } while ((b & 0x80) != 0);
            return value;
        }

        private static int ReadInt32(byte[] data, ref int offset)
        {
            int val = (data[offset] << 24) | (data[offset+1] << 16) | (data[offset+2] << 8) | data[offset+3];
            offset += 4;
            return val;
        }

        private static int ReadInt16(byte[] data, ref int offset)
        {
            int val = (data[offset] << 8) | data[offset+1];
            offset += 2;
            return val;
        }

        private static string ReadString(byte[] data, ref int offset, int length)
        {
            string s = Encoding.ASCII.GetString(data, offset, length);
            offset += length;
            return s;
        }
    }
}
