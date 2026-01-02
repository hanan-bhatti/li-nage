using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Linage.GUI.Helpers;

namespace Linage.GUI.Services
{
    public struct StyleToken
    {
        public int StartIndex;
        public int Length;
        public Color Color;
    }

    public class SyntaxHighlighter
    {
        private readonly RichTextBox _rtb;
        
        // Colors
        private readonly Color ColKeyword = Color.FromArgb(86, 156, 214);
        private readonly Color ColType = Color.FromArgb(78, 201, 176);
        private readonly Color ColString = Color.FromArgb(206, 145, 120);
        private readonly Color ColComment = Color.FromArgb(106, 153, 85);
        private readonly Color ColNormal = Color.FromArgb(212, 212, 212);

        // Regex Patterns
        private const string PatternString = @"""[^""\\]*(?:\\.[^""\\]*)*""";
        private const string PatternComment = @"//.*|/\*[\s\S]*?\*/";
        private const string PatternKeyword = @"\b(abstract|as|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|ref|return|sbyte|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|virtual|void|volatile|while)\b";
        private const string PatternType = @"\b[A-Z][a-zA-Z0-9_]*\b";

        public SyntaxHighlighter(RichTextBox rtb)
        {
            _rtb = rtb;
        }

        // Async parsing method
        public Task<List<StyleToken>> ParseAsync(string text, int globalOffset)
        {
            return Task.Run(() => 
            {
                var tokens = new List<StyleToken>();
                
                // Helper to add tokens
                void AddMatches(string pattern, Color color)
                {
                    foreach (Match m in Regex.Matches(text, pattern, RegexOptions.Multiline))
                    {
                        tokens.Add(new StyleToken 
                        { 
                            StartIndex = globalOffset + m.Index, 
                            Length = m.Length, 
                            Color = color 
                        });
                    }
                }

                // Order matters (last one wins in naive painting, but here we might want to sort)
                // Actually, for RichTextBox sequential painting, last applied color overwrites.
                // So specific rules (comments/strings) should come last or we need to handle overlaps.
                
                // 1. Keywords
                AddMatches(PatternKeyword, ColKeyword);
                
                // 2. Types
                AddMatches(PatternType, ColType);

                // 3. Strings
                AddMatches(PatternString, ColString);
                
                // 4. Comments (Highest priority)
                AddMatches(PatternComment, ColComment);

                return tokens;
            });
        }

        public void ApplyTokens(List<StyleToken> tokens, int rangeStart, int rangeLength)
        {
            if (tokens == null || tokens.Count == 0) return;

            // 1. Stop Painting
            NativeMethods.SuspendDrawing(_rtb);
            
            // 2. Capture State
            int originalStart = _rtb.SelectionStart;
            int originalLength = _rtb.SelectionLength;
            NativeMethods.Point originalScroll = NativeMethods.GetScrollPos(_rtb.Handle);

            try
            {
                // Reset range to normal first (optional, but good for cleanup)
                _rtb.SelectionStart = rangeStart;
                _rtb.SelectionLength = rangeLength;
                _rtb.SelectionColor = ColNormal;

                foreach (var token in tokens)
                {
                    // Boundary check
                    if (token.StartIndex < 0 || token.StartIndex + token.Length > _rtb.TextLength) continue;

                    _rtb.SelectionStart = token.StartIndex;
                    _rtb.SelectionLength = token.Length;
                    _rtb.SelectionColor = token.Color;
                }
            }
            finally
            {
                // 3. Restore State
                _rtb.SelectionStart = originalStart;
                _rtb.SelectionLength = originalLength;
                
                // Force scroll restore
                NativeMethods.Scroll(_rtb.Handle, originalScroll);
                
                // 4. Resume Painting
                NativeMethods.ResumeDrawing(_rtb);
            }
        }

        public async Task HighlightAllAsync()
        {
            string text = _rtb.Text;
            var tokens = await ParseAsync(text, 0);
            ApplyTokens(tokens, 0, text.Length);
        }

        // Synchronous fallback for single line
        public void HighlightLine(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= _rtb.Lines.Length) return;
            int start = _rtb.GetFirstCharIndexFromLine(lineIndex);
            int length = _rtb.Lines[lineIndex].Length;
            
            // Minimal synchronous parsing for immediate feedback
            string text = _rtb.Text.Substring(start, length);
            
            // Just run tokens inline
            var tokens = new List<StyleToken>();
            void Add(string p, Color c) {
                 foreach (Match m in Regex.Matches(text, p)) 
                     tokens.Add(new StyleToken { StartIndex = start + m.Index, Length = m.Length, Color = c });
            }
            
            Add(PatternKeyword, ColKeyword);
            Add(PatternType, ColType);
            Add(PatternString, ColString);
            Add(PatternComment, ColComment);
            
            ApplyTokens(tokens, start, length);
        }
    }
}
