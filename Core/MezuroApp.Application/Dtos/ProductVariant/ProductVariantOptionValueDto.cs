namespace MezuroApp.Application.Dtos.ProductVariant
{
    public class ProductVariantOptionValueDto
    {
        public string Id { get; set; }              // VariantValue Id
        public string OptionValueId { get; set; }   // ProductOptionValue Id

        public string ValueAz { get; set; }
        public string ValueEn { get; set; }
        public string ValueRu { get; set; }
        public string ValueTr { get; set; }
    }
}