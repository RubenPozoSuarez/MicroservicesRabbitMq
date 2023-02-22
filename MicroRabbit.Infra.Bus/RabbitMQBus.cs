using MediatR;
using MicroRabbit.Domain.Core.Bus;
using MicroRabbit.Domain.Core.Commands;
using MicroRabbit.Domain.Core.Events;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace MicroRabbit.Infra.Bus
{
    public sealed class RabbitMQBus : IEventBus
    {
        private readonly RabbitMQSettings _settings;
        private readonly IMediator _mediator;
        private readonly Dictionary<string, List<Type>> _handlers;
        private readonly List<Type> _eventTypes;

        public RabbitMQBus(IMediator mediator, IOptions<RabbitMQSettings> settings)
        {
            _mediator = mediator;
            _handlers = new Dictionary<string, List<Type>>();
            _eventTypes = new List<Type>();
            _settings = settings.Value;
        }

        public void Publish<T>(T @event) where T : Event
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.Hostname,
                UserName = _settings.Username,
                Password = _settings.Password
            };

            using (var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {
                //Nombre del queue
                var eventName = @event.GetType().Name;
                channel.QueueDeclare(eventName, false, false, false, null);
                //Una buena practica es pasar el dato a rabbitMQ en formato JSON
                var message = JsonConvert.SerializeObject(@event);
                
                //Parseamos a bytes porque el servidor de MQ es lo que admite
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish("", eventName, null, body);
            }
        }

        public Task SendCommand<T>(T command) where T : Command
        {
            return _mediator.Send(command);
        }

        //Este metodo podra poder leer los eventos o mensajes que contiene un queue y tambien necesitamos saber que manejadores estan con ese evento
        public void Subscribe<T, TH>()
            where T : Event
            where TH : IEventHandler<T>
        {
            //nombre del queue
            var eventName = typeof(T).Name;
            //Objeto de tipo handler
            var handlerType = typeof(TH);

            //Esta logica con la lista se utiliza para evitar duplicados de eventos
            if(!_eventTypes.Contains(typeof(T)))
                _eventTypes.Add(typeof(T));

            if (!_handlers.ContainsKey(eventName))
                _handlers.Add(eventName, new List<Type>());

            if (_handlers[eventName].Any(s => s.GetType() == handlerType))
                throw new ArgumentException($"El handler exception {handlerType.Name} ya fue registrado anteriormente por '{eventName}'", nameof(handlerType));

            _handlers[eventName].Add(handlerType);
            StartBasicConsume<T>();
        }

        private void StartBasicConsume<T>() where T : Event
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.Hostname,
                UserName = _settings.Username,
                Password = _settings.Password,
                DispatchConsumersAsync = true
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            var eventName = typeof(T).Name;

            channel.QueueDeclare(eventName, false, false, false, null);

            var consumer = new AsyncEventingBasicConsumer(channel);

            //Este es el evento que se va a encargar de consumir los eventos o mensajes.
            //Este evento se disparara cuando un enviio llegue hacia el consumer.
            consumer.Received += Consumer_Received;

            //El mensaje o evento ya fue consumido y puede retirarse del queue
            channel.BasicConsume(eventName, true,  consumer);
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            //Nombre del queue
            var eventName = e.RoutingKey;
            //La data que estamos consumiendo viene en bytes por eso la transformamos
            var message = Encoding.UTF8.GetString(e.Body.Span);

            try
            {
                await ProccessEvent(eventName, message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {

            }
        }

        //Metodo para procesar los mensajes del consumer y a la vez disparar el evento posterior al consumerMensaje
        private async Task ProccessEvent(string eventName, string message)
        {
            //Comprobamos que el manejador contiene el queue
            if(_handlers.ContainsKey(eventName))
            {
                //estas seran las subscripciones que hay en la queue
                var subscriptions = _handlers[eventName];

                //Las recorremos
                foreach (var subscription in subscriptions)
                {
                    //Instanciamos al consumer
                    var handler = Activator.CreateInstance(subscription);
                    if (handler == null)
                        continue;

                    //Buscamos el tipo de evento
                    var eventType = _eventTypes.SingleOrDefault(t => t.Name == eventName);
                    //Convertimos el objeto de tipo json
                    var @event = JsonConvert.DeserializeObject(message, eventType);
                    //concreteType representa al consumer
                    var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);

                    //El consumer disparara el método Handle
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { @event });
                }

            }
                
        }
    }
}
