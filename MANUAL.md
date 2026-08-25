# Manual do Controle de Periféricos

Guia rápido para usar o sistema de controle de hardware/periféricos do coworking.

> **Onde acessar:** https://api-controle-perifericos.vercel.app
> Abra no navegador (Chrome, Edge ou Firefox). Funciona no computador e no celular.

---

## 1. Visão geral

O sistema controla o **estoque de periféricos** (mouses, teclados, cabos, etc.) e registra **quem retirou cada item**. Ele tem três informações principais:

- **Produtos** — os itens de hardware, com o saldo em estoque e o estoque mínimo.
- **Colaboradores** — as pessoas que retiram itens.
- **Movimentações** — todo registro de **entrada**, **saída** ou **ajuste**, que altera o saldo do produto.

Cada pessoa entra com **usuário e senha** e enxerga só o que o seu perfil permite (ver a seção de [Perfis de acesso](#5-perfis-de-acesso-quem-pode-o-quê)).

---

## 2. Entrando no sistema (login)

1. Acesse o endereço do sistema.
2. Digite seu **Usuário** e sua **Senha**.
3. Clique em **Entrar**.

Se aparecer *"Usuário ou senha inválidos"*, confira os dados e tente de novo. Se você não tem usuário, peça a um administrador para criar o seu.

> ⚠️ **Errou a senha várias vezes?** Por segurança, o sistema bloqueia novas tentativas depois de **5 erros em menos de um minuto** e mostra *"Muitas tentativas. Tente novamente em alguns instantes."* Espere cerca de um minuto e tente de novo — não é preciso avisar ninguém.

> 💡 **Esqueceu a senha?** Não há "esqueci minha senha" automático. Peça a um administrador para **resetar** a sua senha: ele cadastra uma nova senha para você e informa qual é. Depois de entrar, troque-a em **Minha conta**.

### A barra de navegação

Depois de entrar, no topo da tela ficam os atalhos: **Início**, **Produtos**, **Colaboradores** e — para administradores — **Movimentações** e **Usuários**. No canto direito aparece seu nome; clicando nele você acessa **Minha conta** ou **Sair**.

---

## 3. As telas e como usá-las

### 3.1. Produtos

Lista todos os periféricos com **Saldo atual**, **Estoque mínimo** e a **Situação**:

- **OK** — saldo **igual ou acima** do mínimo.
- **Abaixo do mínimo** (faixa amarela / etiqueta de alerta) — saldo **menor que** o mínimo, hora de repor.

> O estoque mínimo é o último saldo ainda considerado aceitável, não o primeiro que gera alerta. Com mínimo 5: saldo 5 aparece como **OK**; saldo 4 aparece como **Abaixo do mínimo**.

O que dá para fazer aqui:

- **Buscar por descrição** — digite no campo de busca (ex.: "mouse").
- **Só abaixo do mínimo** — ative o interruptor para ver apenas os itens que precisam de reposição.
- **Novo produto** *(administradores)* — clique no botão, preencha:
  - **Descrição** (obrigatória, até 300 caracteres)
  - **Saldo atual** (número inteiro, não pode ser negativo)
  - **Estoque mínimo** (número inteiro, não pode ser negativo)
  - Clique em **Salvar**.
- **Editar** *(administradores)* — ícone de lápis na linha do produto.
- **Excluir** *(apenas super administradores)* — ícone de lixeira. Pede confirmação; **não dá para desfazer**.

> ⚠️ **Importante:** a tela de edição permite alterar o **Saldo atual**, mas **não é assim que se acerta estoque**. Saldo mudado na edição não deixa rastro de quem mudou, quando nem por quê. Para somar ou subtrair estoque, use sempre **Movimentações** (entrada/saída/ajuste); reserve a edição para corrigir a descrição ou o estoque mínimo.

### 3.2. Colaboradores

Lista as pessoas que retiram itens. São elas que aparecem ao registrar uma **saída**.

- **Buscar por nome** — campo de busca no topo.
- **Novo colaborador** *(administradores)* — informe o **Nome** e clique em **Salvar**.
- **Editar** / **Excluir** — mesmos ícones e mesmas regras dos produtos (excluir é só para super administradores).

### 3.3. Movimentações *(administradores)*

É o coração do controle de estoque. Cada movimentação grava o histórico **e** atualiza o saldo do produto na mesma hora. Há três tipos:

| Tipo | Botão | O que faz | Precisa de colaborador? |
|------|-------|-----------|--------------------------|
| **Entrada** (verde) | `Entrada` | **Soma** ao estoque (chegou item novo) | Não |
| **Saída** (vermelho) | `Saída` | **Subtrai** do estoque (alguém retirou) | **Sim** |
| **Ajuste** (laranja) | `Ajuste` | **Subtrai** do estoque (perda, quebra, item danificado) | Não |

**Como registrar:**

1. Clique no botão do tipo desejado (**Entrada**, **Saída** ou **Ajuste**).
2. Selecione o **Produto** (o campo é pesquisável e mostra o saldo atual ao lado de cada item).
3. Informe a **Quantidade** (número inteiro, maior que zero).
4. Na **saída**, selecione também o **Colaborador** que está retirando.
5. Clique em **Registrar**.

> ⚠️ Em saída e ajuste o sistema **valida o saldo**: se não houver estoque suficiente, a operação é recusada com uma mensagem de erro. Confira o saldo antes.

> ⚠️ Movimentação registrada **não tem botão de editar nem de excluir**, nem para super administradores. A correção existe, mas é feita direto na API por quem cuida tecnicamente do sistema. Por isso, confira os dados antes de clicar em **Registrar**.

**Consultar o histórico:**

- Por padrão, a tela mostra todas as movimentações, da mais recente para a mais antiga, com paginação.
- **Filtrar por produto** ou **por colaborador** — escolha no campo correspondente. A etiqueta azul mostra o que está ativo; clique no "x" dela para remover aquele filtro.
- **Filtrar por tipo** — no campo **Tipo**, escolha **Entrada**, **Saída** ou **Ajuste** para ver só aquele tipo. A opção **Todos** (o padrão) mostra os três juntos.
- **Filtrar por período** — preencha **De** e **Até** e clique em **Filtrar**.
- Os filtros **se combinam**: dá para pedir um colaborador dentro de um período (por exemplo, tudo que o João retirou em julho), só as saídas de um produto, ou produto, colaborador e tipo ao mesmo tempo. O botão **Limpar** zera todos de uma vez.

Cada linha mostra **Data**, **Tipo**, **Produto**, **Quantidade**, **Colaborador** (quando houver) e **Registrado por** (quem fez o lançamento — é o seu usuário).

### 3.4. Minha conta — trocar a própria senha

Disponível para **qualquer usuário**. No menu do seu nome (canto superior direito) → **Minha conta**.

Para trocar a senha você informa a **senha atual** e a **nova senha**. A nova senha precisa ter:

- no mínimo **6 caracteres**;
- ao menos **uma letra minúscula**;
- ao menos **uma letra maiúscula**;
- ao menos **um número**;
- ao menos **um caractere especial** (ex.: `! @ # $ %`).

Exemplo de senha válida: `Coworking@2026`.

### 3.5. Usuários *(administradores)*

Gestão de quem acessa o sistema.

- **Buscar por usuário ou e-mail** — campo de busca no topo.
- **Novo usuário** — cadastra uma pessoa com usuário, e-mail e senha (a senha segue as mesmas regras acima). Por padrão o novo usuário entra com perfil comum.
- **Gerenciar roles** (ícone de engrenagem/escudo) — define os perfis do usuário marcando as caixas. Os perfis disponíveis aqui são **usuário comum** e **administrador**. *Alterar perfis é restrito a super administradores.*
- **Resetar senha** (ícone de cadeado) — abre um formulário onde você **cadastra a nova senha** da pessoa (útil quando alguém esqueceu a senha) e depois a informa a ela. A senha segue as mesmas regras de complexidade. *Um administrador comum não pode resetar a senha de um super administrador.*

> ℹ️ **Super administrador não é um perfil que se concede por aqui.** Ele é definido na configuração do servidor, por quem cuida tecnicamente do sistema. Nenhuma tela promove alguém a super administrador.

---

## 4. Tarefas do dia a dia (passo a passo rápido)

**Chegou item novo no estoque**
→ Movimentações → **Entrada** → escolha o produto → quantidade → **Registrar**.

**Alguém retirou um periférico**
→ Movimentações → **Saída** → produto → quantidade → **colaborador** → **Registrar**.

**Item quebrou / sumiu / foi descartado**
→ Movimentações → **Ajuste** → produto → quantidade → **Registrar**.

**Quero saber o que está acabando**
→ Produtos → ative **Só abaixo do mínimo**.

**Quero ver tudo que uma pessoa pegou**
→ Movimentações → campo **Colaborador** → escolha a pessoa.

**Quero ver o que uma pessoa pegou num período**
→ Movimentações → campo **Colaborador** → escolha a pessoa, preencha **De** e **Até** e clique em **Filtrar**.

**Quero ver só as saídas (ou só as entradas) do mês**
→ Movimentações → campo **Tipo** → **Saída** → preencha **De** e **Até** e clique em **Filtrar**.

**Cadastrar uma pessoa nova que vai retirar itens**
→ Colaboradores → **Novo colaborador**.

**Dar acesso ao sistema para alguém**
→ Usuários → **Novo usuário** (precisa ser administrador).

---

## 5. Perfis de acesso (quem pode o quê)

| Ação | Usuário comum | Administrador | Super administrador |
|------|:---:|:---:|:---:|
| Ver Produtos e Colaboradores | ✅ | ✅ | ✅ |
| Trocar a própria senha | ✅ | ✅ | ✅ |
| Criar / editar Produtos e Colaboradores | — | ✅ | ✅ |
| Registrar Movimentações (entrada/saída/ajuste) | — | ✅ | ✅ |
| Consultar histórico e relatórios | — | ✅ | ✅ |
| Cadastrar usuários e resetar senhas | — | ✅ | ✅ |
| Excluir Produtos / Colaboradores | — | — | ✅ |
| Alterar os perfis (roles) de um usuário | — | — | ✅ |

> Se você clicar em algo que seu perfil não permite, o sistema bloqueia a ação ou nem mostra o botão. Precisa de mais acesso? Fale com um administrador.

> **Sobre o super administrador:** os dois primeiros perfis são atribuídos na tela de **Usuários**. O super administrador, não — a lista de quem tem esse privilégio fica na configuração do servidor e só muda com a ajuda de quem cuida tecnicamente do sistema.

---

## 6. Dúvidas comuns

**O saldo de um produto ficou errado. Como corrijo?**
Registre uma **Entrada** (para somar) ou um **Ajuste** (para subtrair) com a diferença. Assim o histórico fica registrado. Evite "consertar" pela edição do produto: o sistema até deixa, mas depois ninguém consegue saber o que aconteceu.

**Um produto está com saldo igual ao estoque mínimo e não apareceu no alerta. Por quê?**
O alerta só acende quando o saldo fica **abaixo** do mínimo; saldo igual ao mínimo ainda é considerado **OK**. Se quiser ser avisado antes, aumente o estoque mínimo do produto.

**Registrei uma movimentação errada. Como apago?**
Não dá para apagar pelo sistema — a tela de Movimentações não tem edição nem exclusão. Procure quem cuida tecnicamente do sistema, com os detalhes (produto, data, quantidade); a correção é feita direto na API e é restrita a super administradores.

**O login travou com "Muitas tentativas".**
É a proteção contra tentativas de adivinhar senha: são 5 tentativas por minuto. Espere cerca de um minuto e tente de novo. Se você realmente não lembra a senha, peça a um administrador para resetá-la.

**O sistema demorou alguns segundos para abrir.**
Normal de vez em quando: quando fica um tempo sem uso, ele "hiberna" e religa sozinho no primeiro acesso (leva poucos segundos).

**Fui desconectado sozinho.**
Por segurança, a sessão expira após um tempo sem uso. Basta entrar de novo.

---

*Em caso de problemas que este manual não resolve, procure o administrador do sistema.*
