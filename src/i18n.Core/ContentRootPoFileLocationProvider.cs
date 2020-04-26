using System.Collections.Generic;
using System.IO;
using i18n.Core.Abstractions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace i18n.Core
{
    /// <summary>
    /// provides a localization files from the content root folder.
    /// </summary>
    public class ContentRootPoFileLocationProvider : ILocalizationFileLocationProvider
    {
        readonly string _resourcesContainer;
        readonly string _contentRootPath;

        /// <summary>
        /// Creates a new instance of <see cref="ContentRootPoFileLocationProvider"/>.
        /// </summary>
        /// <param name="hostEnvironment"><see cref="IHostEnvironment"/>.</param>
        /// <param name="localizationOptions">The IOptions<LocalizationOptions>.</param>
        public ContentRootPoFileLocationProvider(IHostEnvironment hostEnvironment, IOptions<LocalizationOptions> localizationOptions)
        {
            _contentRootPath = hostEnvironment.ContentRootPath;
            _resourcesContainer = localizationOptions.Value.ResourcesPath;
        }

        /// <inheritdocs />
        public IEnumerable<IFileInfo> GetLocations(string cultureName)
        {
            yield return new PhysicalFileInfo(new FileInfo(Path.Combine(_resourcesContainer, cultureName + ".po")));
            yield return new PhysicalFileInfo(new FileInfo(Path.Combine(_contentRootPath, "locale", cultureName + ".po")));
        }
    }
}
