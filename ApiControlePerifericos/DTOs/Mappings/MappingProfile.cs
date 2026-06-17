using ApiControlePerifericos.Models;
using AutoMapper;

namespace ApiControlePerifericos.DTOs.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Produto, ProdutoDTO>().ReverseMap();
            CreateMap<Colaborador, ColaboradorDTO>().ReverseMap();
            CreateMap<Movimentacao, MovimentacaoDTO>().ReverseMap();

            CreateMap<Movimentacao, MovimentacaoRelatorioDTO>()
                .ForMember(dest => dest.ProdutoDescricao, opt => opt.MapFrom(src => src.Produto.Descricao))
                .ForMember(dest => dest.ColaboradorNome, opt => opt.MapFrom(src => src.Colaborador.Nome))
                .ForMember(dest => dest.TipoDescricao, opt => opt.MapFrom(src => src.Tipo == 'E' ? "Entrada"
                                                                               : src.Tipo == 'S' ? "Saída"
                                                                               : "Ajuste"));
        }
    }
}
