namespace Api.Controllers
{
    using System;
    using System.Linq;

    using Api.IO;

    using Core.Contracts;
    using Core.Contracts.DataService;

    using DTO.DTO;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Net.Http.Headers;
    using System.Collections.Generic;

    public class ImageController : Controller
    {
        private readonly IDataService<ImageDto> dataService;

        private readonly ImageIoService imageIoService;

        private readonly IValidator<IFormFile> validator;

        public ImageController(
            IDataService<ImageDto> dataService,
            IValidator<IFormFile> validator,
            ImageIoService imageIoService)
        {
            this.dataService = dataService;
            this.validator = validator;
            this.imageIoService = imageIoService;
        }

        [Route("api/images")]
        [HttpPost]
        public IActionResult Post()
        {
            try
            {
                if (this.Request.Form.Files.Count == 0)
                {
                    return this.BadRequest();
                }

                var file = this.Request.Form.Files[0];
                var validationErrors = this.validator.Validate(file);

                if (validationErrors.Any())
                {
                    return this.BadRequest(validationErrors);
                }

                var estateId = file.Name;

                var filename = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');

                var fileIdentifier = Guid.NewGuid().ToString();
                this.imageIoService.SaveOnDisk(file, filename, fileIdentifier);

                var link = this.imageIoService.CreateServerLink(this.Request, filename, fileIdentifier);
                var imageId = this.SaveInDb(file, estateId, link);

                return this.Ok(new ImageDto(imageId, estateId, string.Empty, link));
            }
            catch (Exception ex)
            {
                return this.StatusCode(500, ex.Message);
            }
        }

        [Route("api/images/{id}")]
        [HttpDelete]
        public IActionResult Delete(string id)
        {
            var image = this.dataService.GetById(id);
            if (string.IsNullOrEmpty(image.Id))
            {
                return this.NotFound();
            }

            imageIoService.DeleteFromDisk(new List<ImageDto> { image });
            this.dataService.Delete(id);

            return this.Ok();
        }

        private string SaveInDb(IFormFile file, string estateId, string link)
        {
            var imageId = this.dataService.Create(new ImageDto(string.Empty, estateId, file.FileName, link));

            if (string.IsNullOrEmpty(imageId))
            {
                throw new Exception("Could not save iamge in DB");
            }

            return imageId;
        }
    }
}