using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class MerchantData : IFileSpec<MerchantData>, IFileSpec
{
    private static readonly MerchantReader merchantReader = new();
    private static readonly MerchantWriter merchantWriter = new();

    public static string Magic => "MER";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    Merchant? merchant = null;

    public Merchant Merchant => merchant!;

    public MerchantData()
    {

    }

    public MerchantData(Merchant merchant)
    {
        this.merchant = merchant;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        merchant = Merchant.Load(merchantReader, dataReader);
    }

    public void Write(IDataWriter dataWriter)
    {
        if (merchant == null)
            throw new AmbermoonException(ExceptionScope.Application, "Merchant data was null when trying to write it.");

        merchantWriter.WriteMerchant(merchant, dataWriter);
    }
}
