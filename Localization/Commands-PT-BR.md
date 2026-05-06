# Thaumiel Map Editor - Comandos

Esse documento fala sobre todos os comandos, sub-comandos, e as permissões necessárias para executa-los.

## Comandos do Remote Admin 

| Comando | Apelido | Argumentos | Permissões | Descrição |
| :--- | :--- | :--- | :--- | :--- |
| `thaumielmapeditor` | `tme` | Nenhum | Nenhum | Compando pai para os subcomados do Thaumiel Map Editor |

### Sub-comandos

> [!IMPORTANT]
> Esses comandos são executados diretamente pelo Remote Admin usando `tme <sub-commando>`.

| Sub-commando | Apelido | Argumentos | Permissões | Descrição |
| :--- | :--- | :--- | :--- | :--- |
| `convert` | `cv` | <code>&lt;Nome da Esquemática&gt;</code> | `tme.convert` | Converte uma Esquemática do PMER |
| `coroutines` | `coro, cor` | Nenhum | `tme.coroutines` | Lista todas as corrotinas rodando no momento. |
| `destroy` | `de, delete, remove, del` | <code>&lt;Id da Esquemática&gt;</code> | `tme.destroy` | Destroí uma Esquemática específica |
| `grab` | `gr` | <code>&lt;Id da Esquemática&gt;</code> | `tme.grab` | "Segura" uma Esquemática específica |
| `list` | `li` | Nenhum | `tme.list` | Lista todas as Esquemática disponíveis |
| `position` | `pos` | <code>[Id da Esquemática], &lt;Get&#124;Set&gt;, [X], [Y], [Z]</code> | `tme.position` | Muda a posição de uma Esquemática |
| `reload` | `re` | Nenhum | `tme.reload` | Recarrega (``Destroí e Carrega``) todas as Esquemática |
| `rotate` | `rot` | <code>&lt;Id da Esquemática&gt;, &lt;X&gt;, &lt;Y&gt;, &lt;Z&gt;</code> | `tme.rotate` | Muda a rotação de uma Esquemática |
| `save` | Nenhum | <code>&lt;Nome do Mapa&gt;</code> | `tme.save` | Salvas as Esquemática spawNenhums no mapa e converte elas em único arquivo |
| `spawn` | `sp, create, cr` | <code>&lt;Nome da Esquemática/Nome do Mapa&gt;, &lt;X&gt;, &lt;Y&gt;, &lt;Z&gt;</code> | `tme.spawn` | Cria a Esquemática específica no servidor |
| `spawned` | `spd` | Nenhum | `tme.spawned` | Lista todas as Esquemática |

---

## Comandos do Console 

| Comando | Apelido | Descrição |
| :--- | :--- | :--- |
| `tmelogs` | `tmelogsupload, tmeupload` | Envia todos os logs para a API do ``TME`` |
| `tmeupdate` | Nenhum | Atualiza sua versão atual do ``TME`` para a mais recente |
| `tmeupdatecheck` | Nenhum | Checa se existe uma atualização para o ``TME``. |
