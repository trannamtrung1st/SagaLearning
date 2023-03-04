using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SagaLearning.Models;
using SagaLearning.Services;
using SagaLearning.WebApp.Models;

namespace SagaLearning.WebApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IOrderClient _orderClient;
        private readonly IAccountClient _accountClient;
        private readonly IPaymentClient _paymentClient;

        public IndexModel(
            ILogger<IndexModel> logger,
            IOrderClient orderClient,
            IAccountClient accountClient,
            IPaymentClient paymentClient)
        {
            _logger = logger;
            _orderClient = orderClient;
            _accountClient = accountClient;
            _paymentClient = paymentClient;
        }

        public IEnumerable<LogModel> Logs { get; set; }
        public string Message { get; set; }

        public async Task OnGet()
        {
            await Initialize();
        }

        [BindProperty]
        public SubmitOrderModel SubmitOrderModel { get; set; }

        public async Task OnPost()
        {
            if (SubmitOrderModel.Amount > 0)
            {
                Program.PushLog("WebApp", $"[ACTION - Submit order] {DateTime.Now}");

                await _orderClient.SubmitOrder(SubmitOrderModel);

                Message = "Successfully submitted order!";
            }
            else
            {
                Message = "Invalid amount";
            }

            await Initialize();
        }

        public SystemConfigModel Config { get; set; }

        public async Task OnPostUpdateConfig([Bind] SystemConfigModel model)
        {
            await _accountClient.UpdateConfig(new AccountApiConfigModel
            {
                ShouldTransactionFail = model.ShouldTransactionFail
            });

            await _paymentClient.UpdateConfig(new PaymentApiConfigModel
            {
                ShouldInternalPaymentFail = model.ShouldInternalPaymentFail,
                ShouldExternalPaymentFail = model.ShouldExternalPaymentFail,
            });

            await _orderClient.UpdateConfig(new OrderApiConfigModel
            {
                CompleteOrderTryCount = model.CompleteOrderTryCount,
            });

            Message = "Successfully updated config!";

            await Initialize();
        }

        private async Task Initialize()
        {
            var accountConfigTask = _accountClient.GetConfig();
            var paymentConfigTask = _paymentClient.GetConfig();
            var orderConfigTask = _orderClient.GetConfig();

            Logs = Program.Logs.Reverse().ToArray();
            var accountConfig = await accountConfigTask;
            var paymentConfig = await paymentConfigTask;
            var orderConfig = await orderConfigTask;

            Config = new SystemConfigModel
            {
                ShouldTransactionFail = accountConfig.ShouldTransactionFail,
                ShouldExternalPaymentFail = paymentConfig.ShouldExternalPaymentFail,
                ShouldInternalPaymentFail = paymentConfig.ShouldInternalPaymentFail,
                CompleteOrderTryCount = orderConfig.CompleteOrderTryCount,
            };
        }
    }
}