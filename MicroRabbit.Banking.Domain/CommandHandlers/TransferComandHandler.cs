using MediatR;
using MicroRabbit.Banking.Domain.Commands;
using MicroRabbit.Banking.Domain.Events;
using MicroRabbit.Domain.Core.Bus;

namespace MicroRabbit.Banking.Domain.CommandHandlers
{
    public class TransferComandHandler : IRequestHandler<CreateTransferCommand, bool>
    {
        private readonly IEventBus _bus;

        public TransferComandHandler(IEventBus bus)
        {
            _bus = bus;
        }

        public Task<bool> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
        {
            //logica para publicar el mensaje dentro del event bus rabbitmq
            //_bus es el encargado de enviar los datos al rabbit. Publish puede recibir cualquier tipo de objeto(en este caso, clases que hereden de Event)
            //Automaticamente declarara un queue con el nombre de esta clase
            _bus.Publish(new TransferCreatedEvent(request.To, request.From, request.Amount));

            return Task.FromResult(true);
        }
    }
}
