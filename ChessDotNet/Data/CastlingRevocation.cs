using System;

namespace ChessDotNet.Data
{
    public static class CastlingRevocation
    {
        public static CastlingPermission[] Table { get; }

        static CastlingRevocation()
        {
            Table = new CastlingPermission[64];
            for (Byte i = 0; i < 64; i++)
            {
                CastlingPermission permission = CastlingPermission.All;
                switch (i)
                {
                    case 0:
                        permission &= ~CastlingPermission.WhiteQueen;
                        break;
                    case 4:
                        permission &= ~CastlingPermission.WhiteQueen;
                        permission &= ~CastlingPermission.WhiteKing;
                        break;
                    case 7:
                        permission &= ~CastlingPermission.WhiteKing;
                        break;
                    case 56:
                        permission &= ~CastlingPermission.BlackQueen;
                        break;
                    case 60:
                        permission &= ~CastlingPermission.BlackQueen;
                        permission &= ~CastlingPermission.BlackKing;
                        break;
                    case 63:
                        permission &= ~CastlingPermission.BlackKing;
                        break;
                }

                Table[i] = permission;
            }
        }
    }
}