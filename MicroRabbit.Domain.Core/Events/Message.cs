using MediatR;

namespace MicroRabbit.Domain.Core.Events
{
    //Esta clase no podra instanciar objetos pero si sus hijos
    public abstract class Message: IRequest<bool>
    {
        public string MessageType { get; protected set; }

        public Message()
        {
            MessageType = GetType().Name;
        }
    }
}
