using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CassandraAPI.Storage;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CassandraAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PetPhotosController : ControllerBase
    {
        private IPhotoStorage storage;
        public PetPhotosController(IPhotoStorage storage)
        {
            this.storage = storage;
        }

        [EnableCors]
        [HttpGet("{ns}/{localID}/{imNum}/annotated")]
        public async Task<IActionResult> GetAnnotatedImage(string ns, string localID, int imNum) {
            try
            {
                Trace.TraceInformation($"Getting raw photo #{imNum} for {ns}/{localID}");
                var photo = await this.storage.GetPetPhotoAsync(ns, localID, imNum);
                if (photo == null)
                {
                    Trace.TraceInformation($"photo #{imNum} for {ns}/{localID} does not exist. Returning not found");
                    return NotFound();
                }
                else
                {
                    Trace.TraceInformation($"Extracted photo #{imNum} for {ns}/{localID} from storage. Transmitting it to client");
                    string mimeType = photo.AnnotatedImageType switch
                    {
                        "jpg" => "image/jpeg",
                        "png" => "image/png",
                        _ => "image"
                    };
                    return File(photo.AnnotatedImage, mimeType);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception during retrieving raw photo {imNum} for {ns}/{localID}: {ex}");
                return StatusCode(500, ex.ToString());
            }
        }

        // GET: <PetPhotoController>/pet911ru/rf123
        [HttpGet("{ns}/{localID}")]
        [EnableCors]
        public async IAsyncEnumerable<JsonPoco.PetPhoto> GetAll(string ns, string localID, [FromQuery] bool includeBinData=false)
        {
            Trace.TraceInformation($"Getting photos for {ns}/{localID}");
            await foreach (var photo in this.storage.GetPetPhotosAsync(ns, localID, includeBinData))
            {
                if (photo == null) {
                    Trace.TraceInformation($"There are no photos for {ns}/{localID}");
                    // TODO: add NotFound exception here.
                    yield break;
                    throw new KeyNotFoundException($"There are no photos for {ns}/{localID}"); // unreachable code?
                }
                Trace.TraceInformation($"Yielding photo {photo.ImageNum} for {ns}/{localID}");
                yield return new JsonPoco.PetPhoto(photo);
            }
        }

        // PUT <PetPhotoController>/
        [HttpPut("{ns}/{localID}/{imNum}")]
        public async Task<ActionResult> Put(string ns, string localID, int imNum, [FromBody ]JsonPoco.PetPhoto photo)
        {
            try
            {
                Trace.TraceInformation($"Adding photo {imNum} for {ns}/{localID}");
                if (await this.storage.AddPetPhotoAsync(ns, localID, imNum, photo.ToPetPhoto()))
                {
                    Trace.TraceInformation($"successfully added photo {imNum} for {ns}/{localID}");
                    return Ok();
                }
                else
                {
                    Trace.TraceError($"Failed to add photo {imNum} for {ns}/{localID}");
                    return StatusCode(500);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception during adding a photo {imNum} for {ns}/{localID}: {ex}");
                return StatusCode(500, ex.ToString());
            }
        }

        [HttpDelete("{ns}/{localID}/{photoNum?}")]
        public async Task<ActionResult<bool>> Delete(string ns, string localID, int photoNum)
        {
            try
            {
                if (photoNum == 0)
                {
                    // parameter is omitted. thus passing -1 to the storage
                    photoNum = -1;
                }

                Trace.TraceInformation($"Deleting photo {photoNum} for {ns}/{localID}");
                bool res = await this.storage.DeletePetPhoto(ns, localID, photoNum);
                if (res)
                    Trace.TraceInformation($"Successfully deleted photo {photoNum} for {ns}/{localID}");
                else
                    Trace.TraceWarning($"Failed to delete photo {photoNum} for {ns}/{localID}");
                return res;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception during deleting a photo {photoNum} for {ns}/{localID}: {ex}");
                return StatusCode(500, ex.ToString());
            }
        }
    }
}
