////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov 
////////////////////////////////////////////////
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetadataEntityModel
{
    /// <summary>
    /// Абстрактная модель сущности AbstractEntityModel
    /// </summary>
    public class RootEntityModel
    {
        /// <summary>
        /// Ключ/Идентификатор объекта
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Чтение/Запись коментария для объекта [info]
        /// </summary>
        [Display(Name = "Информация", Description = "Произвольная информация к объекту")]
        public string Information { get; set; }

        /// <summary>
        /// Дата создания объекта
        /// </summary>
        [Display(Name = "Дата создания", Description = "Дата/Время создания объекта")]
        public virtual DateTime DateCreate { get; set; }

        /// <summary>
        /// Проверка доступности объекта. Доступный объект должен быть записан в базе данных и не иметь пометок на удаление/отключение
        /// </summary>
        [Display(Name = "Объект доступен", Description = "Доступный объект должен быть записан в базе данных и не иметь пометок на удаление/отключение")]
        public virtual bool IsAvailable => IsDelete == IsOff == false && Id > 0;

        /// <summary>
        /// Проверка/Пометка отметки объекта как [Избранный]
        /// </summary>
        [Display(Name = "Избранное", Description = "Объект помечен как [Избранный]")]
        public bool IsFavorite { get; set; } = false;

        /// <summary>
        /// Проверка/Пометка отключения/включения объекта
        /// </summary>
        [Display(Name = "Отключён", Description = "Объект помечен как [Отключённый]")]
        public bool IsOff { get; set; } = false;

        /// <summary>
        /// Проверка/Пометка удаления объекта
        /// </summary>
        [Display(Name = "Удалён", Description = "Объект помечен как [Удалённый]")]
        public bool IsDelete { get; set; } = false;

        /// <summary>
        /// Сравнение объектов (существующих)
        /// </summary>
        public override bool Equals(object other)
        {
            if (Id == 0 || other is null || other.GetType() != this.GetType())
                return false;

            RootEntityModel norm_other = (RootEntityModel)other;
            if (norm_other.Id == 0)
                return false;

            return this.Id.Equals(norm_other.Id);
        }

        /// <summary>
        /// Get hash code
        /// </summary>
        /// <returns>Returns the hash code for this string.</returns>
        public override int GetHashCode()
        {
            if (Id == 0)
                return 0;

            return (this.GetType().Name + this.Id.ToString()).GetHashCode();
        }

        public static bool operator ==(RootEntityModel a1, RootEntityModel a2)
        {
            if (a1 is null && a2 is null)
                return true;
            else if (a1 is null || a2 is null)
                return false;
            //
            return a1.Equals(a2);
        }

        public static bool operator !=(RootEntityModel a1, RootEntityModel a2)
        {
            if (a1 is null && a2 is null)
                return true;
            else if (a1 is null && !(a2 is null) || a2 is null && !(a1 is null))
                return true;
            //
            return !a1.Equals(a2);
        }
    }
}
