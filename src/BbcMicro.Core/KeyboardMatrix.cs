using System;
using System.Collections.Generic;

namespace BbcMicro.Core
{
    public class KeyboardMatrix
    {
        // 16 rows x 8 columns = 128 keys/links
        private readonly bool[] _keys = new bool[128];
        private readonly Queue<char> _inputQueue = new();
        private int _keyPressTimer = 0;
        private byte _currentInjectedKey = 0xFF;

        public KeyboardMatrix()
        {
            Reset();
        }

        public void Reset()
        {
            Array.Clear(_keys, 0, _keys.Length);
            _inputQueue.Clear();
            _keyPressTimer = 0;
            _currentInjectedKey = 0xFF;
        }

        public void EnqueueText(string text)
        {
            foreach (char c in text)
            {
                _inputQueue.Enqueue(c);
            }
        }

        public void Tick()
        {
            if (_keyPressTimer > 0)
            {
                _keyPressTimer--;
                if (_keyPressTimer == 0 && _currentInjectedKey != 0xFF)
                {
                    _keys[_currentInjectedKey] = false;
                    _currentInjectedKey = 0xFF;
                    _keyPressTimer = 10000; // Delay between keystrokes
                }
                return;
            }

            if (_inputQueue.Count > 0 && _currentInjectedKey == 0xFF)
            {
                char c = _inputQueue.Dequeue();
                byte internalKey = MapCharToInternalKey(c);
                if (internalKey != 0xFF)
                {
                    _currentInjectedKey = internalKey;
                    _keys[internalKey] = true;
                    _keyPressTimer = 30000; // Hold key for ~30,000 cycles (~15ms)
                }
            }
        }

        public void SetKeyDown(byte internalKey, bool down)
        {
            if (internalKey < 128)
                _keys[internalKey] = down;
        }

        public bool HasAnyKeyDown()
        {
            for (int i = 0; i < 128; i++)
            {
                if (_keys[i]) return true;
            }
            return false;
        }

        public bool HasKeyInColumn(byte col)
        {
            col &= 0x0F;
            if (col >= 10) return false;
            for (int row = 0; row < 8; row++)
            {
                int key = (row << 4) | col;
                if (key < 128 && _keys[key])
                    return true;
            }
            return false;
        }

        public byte InterrogateKey(byte keyNum)
        {
            if (keyNum < 128 && _keys[keyNum])
            {
                return 0x80; // Bit 7 = 1 (pressed)
            }
            return 0x00;
        }

        public static byte MapCharToInternalKey(char c)
        {
            return c switch
            {
                'q' or 'Q' => 0x10,
                '3' => 0x11,
                '4' => 0x12,
                '5' => 0x13,
                'f' or 'F' => 0x14, // wait, let's verify F4
                '8' => 0x15,
                '-' => 0x17,
                '^' => 0x18,

                'w' or 'W' => 0x21,
                'e' or 'E' => 0x22,
                't' or 'T' => 0x23,
                '7' => 0x24,
                'i' or 'I' => 0x25,
                '9' => 0x26,
                '_' => 0x28,

                '1' => 0x30,
                '2' => 0x31,
                'd' or 'D' => 0x32,
                'r' or 'R' => 0x33,
                '6' => 0x34,
                'u' or 'U' => 0x35,
                'o' or 'O' => 0x36,
                'p' or 'P' => 0x37,
                '[' => 0x38,

                'a' or 'A' => 0x41,
                'x' or 'X' => 0x42,
                'y' or 'Y' => 0x44,
                'j' or 'J' => 0x45,
                'k' or 'K' => 0x46,
                '@' => 0x47,
                ':' or '*' => 0x48,
                '\r' or '\n' => 0x49, // RETURN

                's' or 'S' => 0x51,
                'c' or 'C' => 0x52,
                'g' or 'G' => 0x53,
                'h' or 'H' => 0x54,
                'b' or 'B' => 0x55,
                'l' or 'L' => 0x56,
                ';' or '+' => 0x57,
                ']' => 0x58,

                'z' or 'Z' => 0x61,
                ' ' => 0x62, // SPACEBAR
                'v' or 'V' => 0x63,
                'n' or 'N' => 0x64,
                'm' or 'M' => 0x65,
                ',' or '<' => 0x66,
                '.' or '>' => 0x67,
                '/' or '?' => 0x68,

                '0' => 0x27,
                '\x1b' => 0x70, // ESCAPE
                _ => 0xFF
            };
        }
    }
}
