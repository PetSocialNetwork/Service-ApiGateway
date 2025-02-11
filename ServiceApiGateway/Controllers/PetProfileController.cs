﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetSocialNetwork.ServicePet;
using PetSocialNetwork.ServicePhoto;
using Service_ApiGateway.Models.Responses;

namespace Service_ApiGateway.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PetProfileController : ControllerBase
    {
        private readonly IPetProfileClient _petProfileClient;
        private readonly IPetPhotoClient _petPhotoClient;
        private readonly IMapper _mapper;
        public PetProfileController(IPetProfileClient petProfileClient,
            IPetPhotoClient petPhotoCleint,
            IMapper mapper)
        {
            _petProfileClient = petProfileClient ?? throw new ArgumentException(nameof(petProfileClient));
            _petPhotoClient = petPhotoCleint ?? throw new ArgumentException(nameof(petPhotoCleint));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(UserProfileWithAccountAlreadyExistsException))]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("[action]")]
        [Consumes("multipart/form-data")]
        public async Task<PetProfileResponse> AddPetProfileAsync(
            [FromForm] AddPetProfileRequest request,
            IFormFile file, CancellationToken cancellationToken)
        {
            //TODO:Транзакция
            var response = await _petProfileClient.AddPetProfileAsync(request, cancellationToken);
            await using var fileStream = file.OpenReadStream();
            var photo = new FileParameter(fileStream, file.FileName, file.ContentType);
            await _petPhotoClient.AddAndSetPetPhotoAsync(response.Id, request.AccountId, photo, cancellationToken);

            return response;
        }

        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(UserProfileNotFoundException))]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("[action]")]
        public async Task<PetProfileResponse> GetPetProfileByIdAsync([FromQuery] Guid id, CancellationToken cancellationToken)
        {
            return await _petProfileClient.GetPetProfileByIdAsync(id, cancellationToken);
        }

        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(UserProfileNotFoundException))]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("[action]")]
        [Consumes("multipart/form-data")]
        public async Task UpdatePetProfileAsync(
            [FromForm] UpdatePetProfileRequest request,
            IFormFile? file, CancellationToken cancellationToken)
        {
            //TODO:Транзакция
            await _petProfileClient.UpdatePetProfileAsync(request, cancellationToken);
            if (file != null)
            {
                await using var fileStream = file.OpenReadStream();
                var photo = new FileParameter(fileStream, file.FileName, file.ContentType);
                await _petPhotoClient.AddAndSetPetPhotoAsync(request.Id, request.AccountId, photo, cancellationToken);
            }
        }

        //[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(UserProfileNotFoundException))]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete("[action]")]
        public async Task DeletePetProfileAsync([FromQuery] Guid petId, [FromQuery] Guid accountId, CancellationToken cancellationToken)
        {
            await _petProfileClient.DeletePetProfileAsync(petId, accountId, cancellationToken);
            await _petPhotoClient.DeleteAllPetPhotosAsync(petId, accountId, cancellationToken);
        }

        [Authorize]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(UserProfileWithAccountAlreadyExistsException))]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("[action]")]
        public async Task<IEnumerable<PetProfileBySearchResponse>> GetPetProfilesByAccountIdAsync([FromBody] Guid accountId, CancellationToken cancellationToken)
        {
            var petProfiles = await _petProfileClient.GetPetProfilesByAccountIdAsync(accountId, cancellationToken);
            List<PetProfileBySearchResponse> result = [];

            if (petProfiles != null)
            {
                foreach (var petProfile in petProfiles)
                {
                    var photo = await _petPhotoClient.GetMainPetPhotoAsync(petProfile.Id, accountId, cancellationToken);
                    var response = _mapper.Map<PetProfileBySearchResponse>(petProfile);
                    response.PhotoUrl = photo.FilePath;
                    result.Add(response);
                }
            }
            return result;
        }      
    }
}
