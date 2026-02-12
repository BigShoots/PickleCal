using System;

namespace PickleCalLG.Meters.Sequences
{
    public enum PatternKind
    {
        None = 0,
        FullField = 1,
        Window = 2
    }

    public readonly struct PatternInstruction
    {
        private PatternInstruction(
            PatternKind kind,
            string description,
            byte red,
            byte green,
            byte blue,
            byte backgroundRed,
            byte backgroundGreen,
            byte backgroundBlue,
            double? windowPercent)
        {
            Kind = kind;
            Description = description;
            Red = red;
            Green = green;
            Blue = blue;
            BackgroundRed = backgroundRed;
            BackgroundGreen = backgroundGreen;
            BackgroundBlue = backgroundBlue;
            WindowPercent = windowPercent;
        }

        public PatternKind Kind { get; }
        public string Description { get; }
        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }
        public byte BackgroundRed { get; }
        public byte BackgroundGreen { get; }
        public byte BackgroundBlue { get; }
        public double? WindowPercent { get; }
        public bool IsValid => Kind != PatternKind.None;

        public static PatternInstruction None => default;

        public static PatternInstruction FullField(string description, byte red, byte green, byte blue)
        {
            description ??= string.Empty;
            return new PatternInstruction(PatternKind.FullField, description, red, green, blue, 0, 0, 0, null);
        }

        public static PatternInstruction Window(
            string description,
            double percent,
            byte red,
            byte green,
            byte blue,
            byte backgroundRed,
            byte backgroundGreen,
            byte backgroundBlue)
        {
            description ??= string.Empty;
            percent = Math.Clamp(percent, 0, 100);
            return new PatternInstruction(PatternKind.Window, description, red, green, blue, backgroundRed, backgroundGreen, backgroundBlue, percent);
        }
    }
}
