// Site JavaScript for genome assembly app

// Function to handle manual correction of consensus sequence
function setupConsensusCorrection() {
    const conflictElements = document.querySelectorAll('.conflict-position');

    conflictElements.forEach(element => {
        element.addEventListener('click', function () {
            const currentPosition = this.getAttribute('data-position');
            const currentBase = this.textContent;

            const newBase = prompt(`Enter new nucleotide for position ${currentPosition} (current: ${currentBase}):`, currentBase);

            if (newBase !== null) {
                // Validate input - only accept A, T, G, C
                if (['A', 'T', 'G', 'C'].includes(newBase.toUpperCase())) {
                    this.textContent = newBase.toUpperCase();
                    alert(`Position ${currentPosition} updated to ${newBase.toUpperCase()}`);
                } else {
                    alert('Invalid input. Please enter A, T, G, or C.');
                }
            }
        });
    });
}

// Function to simulate upload and processing
function simulateUploadAndProcess() {
    const fileInput = document.getElementById('fileInput');
    if (!fileInput.files.length) {
        alert('Please select at least one .ab1 file to upload.');
        return false;
    }

    // Show progress simulation
    const progressBar = document.getElementById('progressBar');
    if (progressBar) {
        progressBar.style.display = 'block';

        let progress = 0;
        const interval = setInterval(() => {
            progress += 10;
            progressBar.style.width = `${progress}%`;

            if (progress >= 100) {
                clearInterval(interval);
                setTimeout(() => {
                    window.location.href = '/Assembly';
                }, 500);
            }
        }, 200);
    }

    return false; // Prevent form submission
}

// Function to handle save project
function saveProject() {
    alert('Project saved successfully!');
}

// Function to handle export to FASTA
function exportFasta() {
    alert('Exporting to FASTA format...');
    // In a real implementation, this would download a file
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    setupConsensusCorrection();
});