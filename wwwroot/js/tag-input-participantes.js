/**
 * TAG INPUT PARTICIPANTES
 * Component para adicionar múltiplos emails (estilo Gmail)
 */

class TagInputParticipantes {
    constructor(inputId, hiddenInputId, maxTags = 50) {
        this.input = document.getElementById(inputId);
        this.hiddenInput = document.getElementById(hiddenInputId);
        this.container = this.input.parentElement;
        this.maxTags = maxTags;
        this.tags = [];
        
        this.init();
    }
    
    /**
     * Inicializar eventos
     */
    init() {
        // Criar container de tags
        const tagsContainer = document.createElement('div');
        tagsContainer.className = 'tags-container';
        tagsContainer.id = 'tags-container';
        
        this.container.insertBefore(tagsContainer, this.input);
        
        // Event: Enter, Tab ou vírgula = adicionar tag
        this.input.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === 'Tab' || e.key === ',') {
                e.preventDefault();
                this.addTag();
            }
            
            // Backspace com input vazio = remover última tag
            if (e.key === 'Backspace' && this.input.value === '') {
                this.removeLastTag();
            }
        });
        
        // Event: Perder foco = adicionar tag
        this.input.addEventListener('blur', () => {
            if (this.input.value.trim()) {
                this.addTag();
            }
        });
    }
    
    /**
     * Adicionar nova tag (email)
     */
    addTag() {
        const email = this.input.value.trim();
        
        // Validar email
        if (!this.isValidEmail(email)) {
            this.showError('Email inválido');
            return;
        }
        
        // Verificar duplicado
        if (this.tags.includes(email)) {
            this.showError('Email já adicionado');
            return;
        }
        
        // Verificar limite
        if (this.tags.length >= this.maxTags) {
            this.showError(`Máximo de ${this.maxTags} participantes`);
            return;
        }
        
        // Adicionar à lista
        this.tags.push(email);
        this.renderTag(email);
        this.updateHiddenInput();
        
        // Limpar input
        this.input.value = '';
        this.input.focus();
        
        console.log('✅ Participante adicionado:', email);
    }
    
    /**
     * Renderizar tag visual
     */
    renderTag(email) {
        const tagsContainer = document.getElementById('tags-container');
        
        const tag = document.createElement('div');
        tag.className = 'participant-tag';
        tag.setAttribute('data-email', email);
        
        const icon = document.createElement('i');
        icon.className = 'bi bi-person-fill';
        
        const span = document.createElement('span');
        span.textContent = email;
        
        const button = document.createElement('button');
        button.type = 'button';
        button.innerHTML = '<i class="bi bi-x"></i>';
        
        // Use addEventListener instead of inline onclick to prevent XSS
        button.addEventListener('click', () => {
            this.removeTag(email);
        });
        
        tag.appendChild(icon);
        tag.appendChild(span);
        tag.appendChild(button);
        
        tagsContainer.appendChild(tag);
    }
    
    /**
     * Remover tag
     */
    removeTag(email) {
        console.log('🗑️ Removendo participante:', email);
        
        this.tags = this.tags.filter(t => t !== email);
        this.updateHiddenInput();
        
        // Re-renderizar todas as tags
        const tagsContainer = document.getElementById('tags-container');
        tagsContainer.innerHTML = '';
        this.tags.forEach(tag => this.renderTag(tag));
    }
    
    /**
     * Remover última tag (Backspace)
     */
    removeLastTag() {
        if (this.tags.length > 0) {
            this.removeTag(this.tags[this.tags.length - 1]);
        }
    }
    
    /**
     * Atualizar campo hidden (JSON)
     */
    updateHiddenInput() {
        // Salvar como JSON
        this.hiddenInput.value = JSON.stringify(this.tags);
        
        // Atualizar contador
        const counter = document.getElementById('participant-counter');
        if (counter) {
            counter.textContent = `${this.tags.length} participante(s)`;
        }
        
        console.log('💾 Participantes:', this.tags);
    }
    
    /**
     * Validar email (regex simples)
     */
    isValidEmail(email) {
        const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return regex.test(email);
    }
    
    /**
     * Mostrar erro temporário
     */
    showError(message) {
        this.input.classList.add('is-invalid');
        
        // Criar/atualizar div de erro
        let errorDiv = this.container.querySelector('.invalid-feedback');
        if (!errorDiv) {
            errorDiv = document.createElement('div');
            errorDiv.className = 'invalid-feedback';
            this.container.appendChild(errorDiv);
        }
        errorDiv.textContent = message;
        errorDiv.style.display = 'block';
        
        // Remover erro após 3 segundos
        setTimeout(() => {
            this.input.classList.remove('is-invalid');
            errorDiv.style.display = 'none';
        }, 3000);
    }
}

// Inicializar quando DOM carregar
let participantesInput;
document.addEventListener('DOMContentLoaded', () => {
    const input = document.getElementById('participantes-input');
    if (input) {
        participantesInput = new TagInputParticipantes(
            'participantes-input',
            'ParticipantesJson'
        );
        console.log('✅ Tag Input Participantes inicializado');
    }
});
