using MicroRabbit.Domain.Core.Bus;
using MicroRabbit.Transfer.Domain.Events;
using MicroRabbit.Transfer.Domain.Interfaces;
using MicroRabbit.Transfer.Domain.Models;

namespace MicroRabbit.Transfer.Domain.EventHandlers
{
    public class TransferEventHandler : IEventHandler<TransferCreatedEvent>
    {
        private readonly ITransferRepository _transferRepository;

        public TransferEventHandler(ITransferRepository transferRepository)
        {
            _transferRepository = transferRepository;
        }

        //Este evento handle es el evento que consuma cuando tengamos un mensaje en el queue
        //En ese momento se disparara un evento que ejecute ese metodo handle y en este evento tu recibes la data de ese queue que ira representado en el @event
        public Task Handle(TransferCreatedEvent @event)
        {
            var transaction = new TransferLog
            {
                FromAccount = @event.From,
                ToAccount = @event.To,
                TransferAmount = @event.Amount
            };

            _transferRepository.AddTransferLog(transaction);


            return Task.CompletedTask;
        }
    }
}
