using System;
using System.Collections.Generic;
using System.Text;

using XRL.World.AI;

namespace XRL.World.AI
{
    [Serializable]
    public class Opinion_UD_WorthyOpponent : IOpinionSubject
    {
        public override bool WantFieldReflection => false;

        public override int BaseValue => -500;

        public override void Write(SerializationWriter Writer)
        {
            Writer.Write(Magnitude);
            Writer.WriteOptimized(Time);
        }

        public override void Read(SerializationReader Reader)
        {
            Magnitude = Reader.ReadSingle();
            Time = Reader.ReadOptimizedInt64();
        }

        public override string GetText(GameObject Actor)
            => "Is a worthy opponent."
            ;
    }
}
