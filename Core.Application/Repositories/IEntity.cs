namespace Core.Application.Repositories;

// Marker (nişan) interfeysi — içində heç nə yoxdur.
// Məqsəd: bütün entity (verilənlər bazası obyektləri) siniflərinin ORTAQ bir tipi
// olsun ki, generic kodda "T : IEntity" kimi məhdudiyyət qoya bilək.
public interface IEntity
{
}

// Yuxarıdakının generic (tipləşdirilmiş) variantı.
// TId — entity-nin Id sahəsinin tipi (məs. int, Guid, string).
// Hər entity-nin unikal açarı (primary key) olmalıdır — bunu məcbur edir.
public interface IEntity<TId> : IEntity
{
    // Obyektin unikal kimliyi (primary key).
    TId Id { get; set; }
}
