using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace RTLolaMo3Vis.Models
{
    public class Message
    {
        public Message(byte[] args)
        {
            this.args = args;
        }

        public void parse()
        {
            int parsedLength = 0;
            changedSeries.Clear();
            changedBoundaries = false;

            while (parsedLength < args.Length)
            {
                //RT
                if (!(args[parsedLength + 0] == (byte)'R' && args[parsedLength + 1] == (byte)'T'))
                {
                    throw new ArgumentException("No RT at beginning");
                }

                // timestamp
                if (!BitConverter.IsLittleEndian) Array.Reverse(args, parsedLength + 2, 8);
                timestamp = BitConverter.ToDouble(args, parsedLength + 2);

                // specID
                specID = args[parsedLength + 10];

                // messageLength
                if (!BitConverter.IsLittleEndian) Array.Reverse(args, parsedLength + 11, 4);
                UInt32 messageLength = BitConverter.ToUInt32(args, parsedLength + 11);
                Debug.WriteLine("message length: " + messageLength);

                //kind
                /*
                switch (args[15])
                {
                    case 1:
                        {
                            parseTrigger(messageLength);
                            parsedLength += (int) messageLength;
                            break;
                            // loop if args not empty
                        }
                    case 2:
                        {
                            parsePlot();
                            parsedLength += (int)messageLength;
                            break;
                            // loop if args not empty
                        }
                    case 3:
                        {
                            parseBoundaries();
                            parsedLength += (int)messageLength;
                            break;
                            // loop if args not empty
                        }
                    default:
                        {
                            Debug.WriteLine("Failed to parse message: kind was" + args[15]);
                            return;
                        }
                }*/
                if (args[parsedLength + 15] == 1)
                {
                    parseTrigger(messageLength, parsedLength);
                    parsedLength += (int)messageLength;
                    continue;
                }
                if (args[parsedLength + 15] == 2)
                {
                    parsePlot(parsedLength);
                    parsedLength += (int)messageLength;
                    continue;
                }
                if (args[parsedLength + 15] == 3)
                {
                    parseBoundaries(parsedLength);
                    parsedLength += (int)messageLength;
                    continue;
                }
            }
        }

        private void parseTrigger(uint messageLength, int parsedLength)
        {

            type = Kind.TRIGGER;

            // triggerId
            triggerId = args[parsedLength + 16];

            // importance
            importance = ImportanceByByte(args[parsedLength + 17]);

            // message
            if (!BitConverter.IsLittleEndian) Array.Reverse(args, parsedLength + 18, (int) messageLength - 18);
            message = Encoding.UTF8.GetString(args, parsedLength + 18, (int) messageLength - 18);
            Debug.WriteLine("Parsed trigger with: " + importance + "Message: " + message);
        }

        private void parsePlot(int parsedLength)
        {
            type = Kind.PLOT;

            plotId = args[parsedLength + 16];

            serieId = args[parsedLength + 17];


            bool triggered = false;
            if (args[parsedLength + 18] == 1) triggered = true;


            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(args, parsedLength + 19, 8);
                Array.Reverse(args, parsedLength + 27, 8);
            }

            double x = BitConverter.ToDouble(args, parsedLength + 19);

            double y = BitConverter.ToDouble(args, parsedLength + 27);

            changedSeries.Add((plotId, serieId, x, y, triggered));
            Debug.WriteLine("Parsed point");
        }

        private void parseBoundaries(int parsedLength)
        {
            plotId = args[parsedLength + 16];

            changedBoundaries = true;

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(args, parsedLength + 17, 8);
                Array.Reverse(args, parsedLength + 25, 8);
                Array.Reverse(args, parsedLength + 33, 8);
                Array.Reverse(args, parsedLength + 41, 8);
            }

            xMin = BitConverter.ToDouble(args, parsedLength + 17);

            xMax = BitConverter.ToDouble(args, parsedLength + 25);

            yMin = BitConverter.ToDouble(args, parsedLength + 33);

            yMax = BitConverter.ToDouble(args, parsedLength + 41);

            Debug.WriteLine("Parsed Boundaries");
        }

        private double timestamp;
        private byte specID;
        private byte triggerId;
        private Importance importance;
        private string message;

        private byte plotId;
        private byte serieId;

        private double xMin;
        private double yMin;

        private double xMax;
        private double yMax;

        private Kind type;

        private byte[] args;

        private bool plot = true;

        public Kind Type => type;

        public double Timestamp => timestamp;

        public byte Specification => specID;

        public byte TriggerId => triggerId;

        public Importance Importance => importance;

        public string Net_Message => message;

        public string SerieName => serieId.ToString();

        public double XMin => xMin;

        public double YMin => yMin;

        public double XMax => xMax;

        public double YMax => yMax;

        public byte PlotId => plotId;

        public List<(byte plot, byte serie, double x, double y, bool triggered)> ChangedSeries => changedSeries;

        private readonly List<(byte plot, byte serie, double x, double y, bool triggered)> changedSeries = new List<(byte plot, byte serie, double x, double y, bool triggered)>();

        public bool ChangedBoundaries;

        private bool changedBoundaries;

        public static Importance ImportanceByByte(byte id) => id switch
        {
            0 => Importance.ADVISORY,
            1 => Importance.CAUTION,
            2 => Importance.WARNING,
            3 => Importance.ALERT,
            4 => Importance.ERROR,
            _ => Importance.NOT_KNOWN
        };

        public enum Kind
        {
            TRIGGER,
            PLOT
        }
    }
}
