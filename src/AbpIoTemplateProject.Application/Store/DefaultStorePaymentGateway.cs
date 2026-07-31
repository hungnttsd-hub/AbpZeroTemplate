using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AbpIoTemplateProject.Store;

/// <summary>
/// Safe default adapter. Production payment providers can replace this implementation
/// without changing order or checkout logic.
/// </summary>
public class DefaultStorePaymentGateway : IStorePaymentGateway, ITransientDependency
{
    public bool CanHandle(PaymentMethod method)
    {
        return method is PaymentMethod.BankTransfer or PaymentMethod.Online;
    }

    public Task<PaymentGatewayResult> InitializeAsync(PaymentGatewayRequest request)
    {
        var result = new PaymentGatewayResult
        {
            Reference = $"PAY-{request.OrderNumber}",
            Instructions = request.Method == PaymentMethod.BankTransfer
                ? $"Chuyển khoản với nội dung {request.OrderNumber}. Đơn hàng được xử lý sau khi đối soát."
                : "Cổng thanh toán trực tuyến chưa được cấu hình cho môi trường này. Đơn hàng đang chờ thanh toán."
        };
        return Task.FromResult(result);
    }
}
