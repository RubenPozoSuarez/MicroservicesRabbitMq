
namespace MicroRabbit.Banking.Domain.Commands
{
    public class CreateTransferCommand : TransferComand
    {
        public CreateTransferCommand(int from, int to, decimal amount)
        {
            From = from;
            To = to;
            Amount = amount;
        }
    }
}
