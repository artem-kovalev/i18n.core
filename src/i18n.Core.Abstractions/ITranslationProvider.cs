using System.Globalization;

namespace i18n.Core.Abstractions
{
    /// <summary>
    /// Contract to provide a translations.
    /// </summary>
    public interface ITranslationProvider
    {
        /// <summary>
        /// Loads translations from a certain source for a specific culture.
        /// </summary>
        /// <param name="cultureName">The culture name.</param>
        /// <param name="dictionary">The <see cref="CultureDictionary"/> that will contains all loaded translations.</param>
        void LoadTranslations(CultureInfo cultureName, CultureDictionary dictionary);
    }
}
