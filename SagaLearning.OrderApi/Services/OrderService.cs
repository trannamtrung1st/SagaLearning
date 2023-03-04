using SagaLearning.Models;
using SagaLearning.OrderApi.Entities;
using SagaLearning.OrderApi.Persistence;

namespace SagaLearning.OrderApi.Services
{
    public interface IOrderService
    {
        IEnumerable<OrderModel> GetOrders();
        OrderModel CreateOrder(SubmitOrderModel model);
        void MarkOrderAsFailed(Guid orderId, string failureDetails);
        void MarkOrderAsSuccess(Guid orderId);
    }

    public class OrderService : IOrderService
    {
        private readonly OrderDbContext _dbContext;

        public OrderService(OrderDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public OrderModel CreateOrder(SubmitOrderModel model)
        {
            if (model.Amount <= 0)
            {
                throw new ArgumentException(nameof(model.Amount));
            }

            var entity = new OrderEntity
            {
                Amount = model.Amount,
                CreationTime = DateTime.Now,
                Id = Guid.NewGuid(),
                Status = "Processing",
                FailureDetails = null
            };

            _dbContext.Order.Add(entity);

            Thread.Sleep(2000); // [NOTE] demo

            _dbContext.SaveChanges();

            return new OrderModel
            {
                Id = entity.Id,
                Amount = entity.Amount,
                CreationTime = entity.CreationTime,
                FailureDetails = entity.FailureDetails,
                Status = entity.Status
            };
        }

        public IEnumerable<OrderModel> GetOrders()
        {
            var orders = _dbContext.Order
                .OrderByDescending(o => o.CreationTime)
                .Select(o => new OrderModel
                {
                    Id = o.Id,
                    Amount = o.Amount,
                    Status = o.Status,
                    FailureDetails = o.FailureDetails,
                    CreationTime = o.CreationTime
                }).ToArray();

            return orders;
        }

        public void MarkOrderAsFailed(Guid orderId, string failureDetails)
        {
            var order = _dbContext.Order.Find(orderId);

            order.Status = "Failed";
            order.FailureDetails = failureDetails;

            Thread.Sleep(2000); // [NOTE] demo

            _dbContext.SaveChanges();
        }

        public void MarkOrderAsSuccess(Guid orderId)
        {
            var order = _dbContext.Order.Find(orderId);

            order.Status = "Successful";

            Thread.Sleep(2000); // [NOTE] demo

            _dbContext.SaveChanges();
        }
    }
}
