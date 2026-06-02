namespace Stylebazar.DAL.Entities.Enums;

// MƏHSULUN VƏZİYYƏTİ enum-u — məhsul yeni, işlənmiş, yoxsa vintage-dir?
public enum ProductCondition
{
    New = 0,         // Yeni
    SecondHand = 1,  // İşlənmiş (ikinci əl)
    Vintage = 2      // Vintage (köhnə / kolleksiya)
}
