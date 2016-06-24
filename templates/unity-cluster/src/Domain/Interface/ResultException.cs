using System;
using ProtoBuf;
using TypeAlias;

namespace Domain
{
    public enum ResultCodeType
    {
        None = 0,
        LoginFailed,
        NicknameInvalid,
        NoteInvalid,
        NoteDuplicate,
        NoteNotFound
    }

    [ProtoContract, TypeAlias]
    public class ResultException : Exception
    {
        [ProtoMember(1)] public ResultCodeType ResultCode;

        public ResultException()
        {
        }

        public ResultException(ResultCodeType resultCode)
        {
            ResultCode = resultCode;
        }

        public override string ToString()
        {
            return string.Format("!{0}!", ResultCode);
        }
    }
}
