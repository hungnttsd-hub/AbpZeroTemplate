namespace AbpIoTemplateProject.Store;

public enum ProductType
{
    Simple = 0,
    Variant = 1
}

public enum InventoryTransactionType
{
    Receive = 0,
    Issue = 1,
    Adjust = 2,
    TransferIn = 3,
    TransferOut = 4,
    Reserve = 5,
    Release = 6,
    Sale = 7,
    Return = 8
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Preparing = 2,
    ReadyToShip = 3,
    Shipping = 4,
    Completed = 5,
    Cancelled = 6,
    ReturnRequested = 7,
    Returned = 8
}

public enum PaymentStatus
{
    Unpaid = 0,
    Pending = 1,
    Paid = 2,
    PartiallyPaid = 3,
    Failed = 4,
    RefundPending = 5,
    PartiallyRefunded = 6,
    Refunded = 7
}

public enum PaymentMethod
{
    CashOnDelivery = 0,
    BankTransfer = 1,
    Online = 2
}

public enum PromotionType
{
    Percentage = 0,
    FixedAmount = 1
}

public enum ContentStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public enum HomeSectionType
{
    FeaturedProducts = 0,
    BestSellers = 1,
    NewProducts = 2,
    Promotions = 3,
    Category = 4,
    Brand = 5,
    Articles = 6,
    Stores = 7
}
